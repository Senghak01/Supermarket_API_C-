using Supermarket.API.Models;

namespace Supermarket.API.Services.Interfaces;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(Customer customer);
}
