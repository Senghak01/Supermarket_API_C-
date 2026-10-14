using Supermarket.API.DTOs.Profile;

namespace Supermarket.API.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public CustomerResponse Customer { get; set; } = null!;
}
