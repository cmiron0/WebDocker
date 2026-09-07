using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using WebDocker.Data;

namespace WebDocker.Services
{
    public class PermissionClaimsTransformacion : IClaimsTransformation
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<PermissionService> _logger;
        private readonly PermissionService _permissionService;

        public PermissionClaimsTransformacion(UserManager<AppUser> userManager, ILogger<PermissionService> logger, PermissionService permissionService)
        {
            _userManager = userManager;
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            // Si no está autenticado o ya tiene permisos cargados (JWT ya los trae), no hacemos nada.
            if (principal.Identity is not { IsAuthenticated: true }) return principal;
            if (principal.HasClaim(c => c.Type == AppClaimTypes.Permission)) return principal;

            // User
            var user = await _userManager.GetUserAsync(principal);
            if (user is null) return principal;

            // Permission Claims - Permissions (RBAC) del usuario
            var claims = await _permissionService.GetPermissionClaims(user);
            principal.AddIdentity(new ClaimsIdentity(claims));

            return principal;
        }
    }
}
