using System.ComponentModel.DataAnnotations;

namespace Supermarket.API.DTOs.Address;

public class CreateAddressRequest
{
    [Required, StringLength(30)]
    public string Label { get; set; } = "Home";

    [Required, StringLength(100)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Phone, StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string AddressLine { get; set; } = string.Empty;

    [StringLength(100)]
    public string? District { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Note { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    public bool IsDefault { get; set; }
}
