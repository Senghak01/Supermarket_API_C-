using Supermarket.API.DTOs.Profile;

namespace Supermarket.API.Services.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> GetProfileAsync(int customerId);
    Task<CustomerResponse> UpdateProfileAsync(int customerId, UpdateProfileRequest request);
}
