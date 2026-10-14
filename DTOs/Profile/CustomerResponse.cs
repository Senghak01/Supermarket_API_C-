namespace Supermarket.API.DTOs.Profile;

public class CustomerResponse
{
    public int CustomerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
