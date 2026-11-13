using System.Security.Claims;

namespace NasaMoon.Api;

/// <summary>Reads the logged-in researcher's id/role from the JWT claims.</summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>User id from the "sub" claim, or null for anonymous requests.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal user)
        => user.IsInRole("Admin");
}
