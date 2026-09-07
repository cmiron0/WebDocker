using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebDocker.Data;

namespace WebDocker.Services;

public class JwtTokenService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<JwtTokenService> _logger;
    private readonly PermissionService _permissionService;
    private readonly IConfiguration _configuration;

    public JwtTokenService(UserManager<AppUser> userManager, ILogger<JwtTokenService> logger,  PermissionService permissionService, IConfiguration configuration)
    {
        _userManager = userManager;
        _logger = logger;
        _permissionService = permissionService;
        _configuration = configuration;

    }

    // El parámetro permissions es opcional
    //public string GenerateToken(AppUser user, IList<string> roles, IList<string>? permissions = null)
    //{
    //    var issuer = _configuration["Jwt:Issuer"]!;
    //    var audience = _configuration["Jwt:Audience"]!;
    //    var keyText = _configuration["Jwt:Key"]!;
    //    var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

    //    var claims = new List<Claim>
    //    {
    //        new(JwtRegisteredClaimNames.Sub, user.Id),
    //        new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
    //        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    //    };

    //    foreach (var role in roles)
    //    {
    //        claims.Add(new Claim(ClaimTypes.Role, role));
    //    }

    //    // Un claim "permission" por cada permiso. Los permisos viajan como claims en el token.
    //    if (permissions != null)
    //    {
    //        foreach (var permission in permissions)
    //        {
    //            claims.Add(new Claim("permission", permission));
    //        }
    //    }

    //    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
    //    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    //    var token = new JwtSecurityToken(
    //        issuer: issuer,
    //        audience: audience,
    //        claims: claims,
    //        expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
    //        signingCredentials: credentials);

    //    return new JwtSecurityTokenHandler().WriteToken(token);
    //}

    public async Task<string> GenerateToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Roles
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        // Custom Claims
        //claims.AddRange(await _userManager.GetClaimsAsync(user));

        // Permission Claims - Permissions (RBAC) del usuario
        claims.AddRange(await _permissionService.GetPermissionClaims(user));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

}
