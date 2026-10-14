using System.Security.Claims;
using Supermarket.API.Exceptions;

namespace Supermarket.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetCustomerId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(value, out var id))
            throw AppException.Unauthorized("Invalid token.");
        return id;
    }
}
