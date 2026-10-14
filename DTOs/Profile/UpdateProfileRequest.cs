using System.ComponentModel.DataAnnotations;

namespace Supermarket.API.DTOs.Profile;

public class UpdateProfileRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
}
