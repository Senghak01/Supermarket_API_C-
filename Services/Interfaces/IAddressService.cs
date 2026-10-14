using Supermarket.API.DTOs.Address;

namespace Supermarket.API.Services.Interfaces;

public interface IAddressService
{
    Task<List<AddressResponse>> GetAllAsync(int customerId);
    Task<AddressResponse> GetByIdAsync(int customerId, int addressId);
    Task<AddressResponse> CreateAsync(int customerId, CreateAddressRequest request);
    Task<AddressResponse> UpdateAsync(int customerId, int addressId, UpdateAddressRequest request);
    Task DeleteAsync(int customerId, int addressId);
    Task<AddressResponse> SetDefaultAsync(int customerId, int addressId);
}
