using Supermarket.API.Models;

namespace Supermarket.API.DTOs.Address;

public class AddressResponse
{
    public int AddressId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? District { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Note { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }

    public static AddressResponse From(CustomerAddress a) => new()
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
