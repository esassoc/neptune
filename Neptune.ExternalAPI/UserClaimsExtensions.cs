using System.Security.Claims;

namespace Neptune.ExternalAPI;

public static class UserClaimsExtensions
{
    public static int? GetPersonID(this ClaimsPrincipal user)
    {
        var personIDClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(personIDClaim, out var personID) ? personID : null;
    }

    public static int? GetRoleID(this ClaimsPrincipal user)
    {
        var roleIDClaim = user.FindFirst("RoleID")?.Value;
        return int.TryParse(roleIDClaim, out var roleID) ? roleID : null;
    }
}
