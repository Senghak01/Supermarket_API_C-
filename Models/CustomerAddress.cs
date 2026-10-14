namespace Supermarket.API.Models;

public class CustomerAddress
{
    public int AddressId { get; set; }
    public int CustomerId { get; set; }

    public string Label { get; set; } = "Home";          // Home, Work, ...
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Note { get; set; }                    // delivery note

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool IsDefault { get; set; }
    public bool IsDeleted { get; set; }                  // soft delete: orders keep pointing at the address
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Customer Customer { get; set; } = null!;
}
