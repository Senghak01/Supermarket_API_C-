using Microsoft.EntityFrameworkCore;
using Supermarket.API.Data;
using Supermarket.API.DTOs.Profile;
using Supermarket.API.Exceptions;
using Supermarket.API.Services.Interfaces;

namespace Supermarket.API.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;

    public CustomerService(ApplicationDbContext db) => _db = db;

    public async Task<CustomerResponse> GetProfileAsync(int customerId)
    {
        var c = await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CustomerId == customerId)
            ?? throw AppException.NotFound("Customer not found.");

        return new CustomerResponse { CustomerId = c.CustomerId, Username = c.Username, Email = c.Email };
    }

    public async Task<CustomerResponse> UpdateProfileAsync(int customerId, UpdateProfileRequest request)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.CustomerId == customerId)
            ?? throw AppException.NotFound("Customer not found.");

        c.Username = request.Username.Trim();
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new CustomerResponse { CustomerId = c.CustomerId, Username = c.Username, Email = c.Email };
    }
}
