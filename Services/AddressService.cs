using Microsoft.EntityFrameworkCore;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Address;
using Supermarket.API.Exceptions;
using Supermarket.API.Models;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class AddressService : IAddressService
{
    private const int MaxAddresses = 10;
    private readonly ApplicationDbContext _db;

    public AddressService(ApplicationDbContext db) => _db = db;

    public async Task<List<AddressResponse>> GetAllAsync(int customerId)
    {
        var list = await _db.CustomerAddresses.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return list.Select(ToResponse).ToList();
    }

    public async Task<AddressResponse> GetByIdAsync(int customerId, int addressId)
        => ToResponse(await FindOwnedAsync(customerId, addressId, track: false));

    public async Task<AddressResponse> CreateAsync(int customerId, CreateAddressRequest request)
    {
        var existing = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId)
            .ToListAsync();

        if (existing.Count >= MaxAddresses)
            throw new AppException($"You can save up to {MaxAddresses} addresses.");

        var address = new CustomerAddress { CustomerId = customerId };
        Apply(address, request);

        // First address is always the default
        address.IsDefault = existing.Count == 0 || request.IsDefault;
        if (address.IsDefault)
            foreach (var a in existing) a.IsDefault = false;

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync();
        return ToResponse(address);
    }

    public async Task<AddressResponse> UpdateAsync(int customerId, int addressId, UpdateAddressRequest request)
    {
        var address = await FindOwnedAsync(customerId, addressId, track: true);
        Apply(address, request);
        address.UpdatedAt = DateTime.UtcNow;

        if (request.IsDefault && !address.IsDefault)
        {
            await ClearDefaultsAsync(customerId, exceptId: addressId);
            address.IsDefault = true;
        }
        // Un-setting the default directly is not allowed; set another one as default instead.

        await _db.SaveChangesAsync();
        return ToResponse(address);
    }

    public async Task DeleteAsync(int customerId, int addressId)
    {
        var address = await FindOwnedAsync(customerId, addressId, track: true);
        var wasDefault = address.IsDefault;

        // Soft delete: orders keep pointing at this address
        address.IsDeleted = true;
        address.IsDefault = false;
        address.UpdatedAt = DateTime.UtcNow;

        if (wasDefault)
        {
            var next = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.AddressId != addressId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            if (next is not null) next.IsDefault = true;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<AddressResponse> SetDefaultAsync(int customerId, int addressId)
    {
        var address = await FindOwnedAsync(customerId, addressId, track: true);
        await ClearDefaultsAsync(customerId, exceptId: addressId);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToResponse(address);
    }

    // ---- helpers ----

    // Ownership check: always filter by customerId. Not found => 404 (never reveals other users' IDs).
    private async Task<CustomerAddress> FindOwnedAsync(int customerId, int addressId, bool track)
    {
        var query = track ? _db.CustomerAddresses : _db.CustomerAddresses.AsNoTracking();
        return await query.FirstOrDefaultAsync(a => a.AddressId == addressId && a.CustomerId == customerId)
            ?? throw AppException.NotFound("Address not found.");
    }

    private async Task ClearDefaultsAsync(int customerId, int exceptId)
    {
        var others = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId && a.IsDefault && a.AddressId != exceptId)
            .ToListAsync();
        foreach (var a in others) a.IsDefault = false;
    }

    private static void Apply(CustomerAddress a, CreateAddressRequest r)
    {
        a.Label = r.Label.Trim();
        a.RecipientName = r.RecipientName.Trim();
        a.PhoneNumber = r.PhoneNumber.Trim();
        a.AddressLine = r.AddressLine.Trim();
        a.District = r.District?.Trim();
        a.City = r.City.Trim();
        a.Note = r.Note?.Trim();
        a.Latitude = r.Latitude;
        a.Longitude = r.Longitude;
    }

    private static AddressResponse ToResponse(CustomerAddress a) => new()
    {
        AddressId = a.AddressId,
        Label = a.Label,
        RecipientName = a.RecipientName,
        PhoneNumber = a.PhoneNumber,
        AddressLine = a.AddressLine,
        District = a.District,
        City = a.City,
        Note = a.Note,
        Latitude = a.Latitude,
        Longitude = a.Longitude,
        IsDefault = a.IsDefault
    };
}
