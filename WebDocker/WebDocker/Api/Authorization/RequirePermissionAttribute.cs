using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebDocker.Api.Authorization;

// Atributo que se pone sobre una acción del controlador
// [RequirePermission("servers.read")]...
// Implementa IAuthorizationFilter: ASP.NET lo ejecuta ANTES de entrar en la acción.
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission) => _permission = permission;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // El usuario esta autenticado?
        // El [Authorize] del controlador ya lo garantiza, pero comprobarlo aquí lo hace más robusto.
        if (user?.Identity is null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();      // 401 No autenticado
            return;
        }

        // El token trae el claim "permission" con el código que pedimos
        var tienePermiso = user.Claims.Any(c => c.Type == "permission" && c.Value == _permission);

        // Si no lo tiene, 403 (autenticado pero sin permiso).
        if (!tienePermiso)
        {
            context.Result = new ForbidResult();            // 403 Prohibido
        }
        // Si lo tiene, no hacemos nada y la petición continúa hacia la acción.
    }
}
