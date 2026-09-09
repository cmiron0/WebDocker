using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebDocker.Api.Dtos;
using WebDocker.Data;
using WebDocker.Services;

namespace WebDocker.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly JwtTokenService _jwtTokenService;
    private readonly PermissionService _permissionService;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLogService;

    public AuthController(UserManager<AppUser> userManager, ILogger<AuthController> logger, JwtTokenService jwtTokenService,
        PermissionService permissionService, IConfiguration configuration, AppDbContext db, AuditLogService auditLogService)
    {
        _userManager = userManager;
        _logger = logger;
        _jwtTokenService = jwtTokenService;
        _permissionService = permissionService;
        _configuration = configuration;
        _db = db;
        _auditLogService = auditLogService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
        {
            string msg = "Usuario o contraseña incorrectos";
            await _auditLogService.AppLog(request.UserName, "LogIn", "AuthController", false, $"{msg} Usuario incorrecto.");
            return Unauthorized(new { message = msg });
        }

        var passwordOk = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordOk)
        {
            string msg = "Usuario o contraseña incorrectos";
            await _auditLogService.AppLog(request.UserName, "LogIn", "AuthController", false, $"{msg} Contraseña incorrecto.");
            return Unauthorized(new { message = msg });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionCodes(user);
        var token = await _jwtTokenService.GenerateToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);
        var expiration = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        await _auditLogService.AppLog(user.UserName!, "LogIn", "AuthController", true, "Ok");

        return Ok(await GetLoginResponse(user));
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<UserInfoResponseDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null)
        {
            await _auditLogService.AppLog("", "Me", "AuthController", false, "Unauthorized");
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await _auditLogService.AppLog("", "Me", "AuthController", false, "NotFound");
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionCodes(user);

        await _auditLogService.AppLog(user.UserName!, "Me", "AuthController", true, "Ok");

        return Ok(new UserInfoResponseDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            Permissions = permissions
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponseDto>> Refresh(RefreshRequestDto request)
    {
        // Buscamos el refresh token en BD.
        var storedRefreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

        // Comprobamos que existe, no revocado y no caducado.
        if (storedRefreshToken == null || storedRefreshToken.Revoked || storedRefreshToken.ExpiresAt < DateTime.UtcNow)
        {
            string msg = "Refresh token inválido o caducado";
            await _auditLogService.AppLog(storedRefreshToken?.UserId ?? "Desconocido", "Refresh", "AuthController", false, $"Unauthorized: {msg}");
            return Unauthorized(new { message = msg });
        }

        // Buscamos el usuario y emitimos un access token nuevo.
        var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId);
        if (user == null)
        {
            await _auditLogService.AppLog(storedRefreshToken?.UserId ?? "Desconocido", "Refresh", "AuthController", false, "Unauthorized");
            return Unauthorized();
        }

        await _auditLogService.AppLog(user.UserName!, "Refresh", "AuthController", true, "Ok");

        return Ok(await GetLoginResponse(user));

    }
        
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register(RegisterRequestDto request)
    {
        var existing = await _userManager.FindByNameAsync(request.UserName);
        if (existing != null)
        {
            string msg = "El nombre de usuario ya está en uso";
            await _auditLogService.AppLog(request.UserName, "Register", "AuthController", false, $"{msg}");
            return Conflict(new { message = msg });
        }

        var user = new AppUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            await _auditLogService.AppLog(request.UserName, "Register", "AuthController", false, $"Create User {errors}");
            return BadRequest(new { message = errors });
        }

        await _userManager.AddToRoleAsync(user, "Usuario");
        await _auditLogService.AppLog(user.UserName!, "Register", "AuthController", true, "Ok");
        return Ok(await GetLoginResponse(user));
    }


    // --------------------------------------------------------------------------------------------
    // --------------------------------------------------------------------------------------------
    // Crea un refresh token aleatorio, lo guarda en BD y devuelve su cadena.
    private async Task<string> GenerateRefreshToken(string userId)
    {
        var refreshToken = new RefreshToken
        {
            Token = await _jwtTokenService.GenerateRefreshToken(),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();
        return refreshToken.Token;
    }

    // Crear el LoginResponse para el usuario. (Login, Refresh, Register)
    private async Task<LoginResponseDto> GetLoginResponse(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await _permissionService.GetPermissionCodes(user);
        var token = await _jwtTokenService.GenerateToken(user);
        var refreshToken = await GenerateRefreshToken(user.Id);
        var expiration = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        return new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            UserName = user.UserName ?? string.Empty,
            Roles = roles,
            Permissions = permissions,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiration)
        };
    }
}
