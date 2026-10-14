namespace Supermarket.API.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }

    // Google / Facebook / null
    public string? Provider { get; set; }
    // ID from Google or Facebook
    public string? ProviderId { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public Cart? Cart { get; set; }
}
