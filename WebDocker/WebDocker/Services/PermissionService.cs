using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebDocker.Data;

namespace WebDocker.Services;

public static class AppClaimTypes
{
    public const string Permission = "permission";
}

public static class ClaimsPrincipalExtensions
{
    public static bool HasPermission(this ClaimsPrincipal user, string code) => user.HasClaim(AppClaimTypes.Permission, code);
}


// Permisos por usuario. Se calcula a partir de sus roles:
// usuario -> roles -> RolePermissions -> Permissions.
public class PermissionService
{
    
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<PermissionService> _logger;
    private readonly AppDbContext _db;

    public PermissionService(UserManager<AppUser> userManager, ILogger<PermissionService> logger, AppDbContext db)
    {
        _userManager = userManager;
        _logger = logger;
        _db = db;
    }

    // Devuelve la lista de códigos de permiso del usuario.
    public async Task<List<string>> GetPermissionCodes(AppUser user)
    {
        // Roles del usuario
        var roleNames = await _userManager.GetRolesAsync(user);

        // Ids de esos roles (RolePermission usa el Id del rol, no el nombre).
        var roleIds = await _db.Roles
            .Where(r => roleNames.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync();

        // Permisos asociados a esos roles, sin duplicados.
        var permissionCodes = await _db.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToListAsync();

        return permissionCodes;
    }

    public async Task<IList<Claim>> GetPermissionClaims(AppUser user)
    {
        var codes = await GetPermissionCodes(user);
        return codes.Select(c => new Claim(AppClaimTypes.Permission, c)).ToList();
    }
}
