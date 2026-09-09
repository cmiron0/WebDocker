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
