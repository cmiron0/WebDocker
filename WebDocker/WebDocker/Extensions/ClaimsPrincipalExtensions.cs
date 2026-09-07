using System.Security.Claims;

namespace WebDocker.Extensions;

public static class ClaimsPrincipalExtensions
{
    public const string PERMISSION = "permission";

    public static bool HasPermission(this ClaimsPrincipal user, string code)
        => user.HasClaim(PERMISSION, code);
}
