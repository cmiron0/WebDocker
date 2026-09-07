using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebDocker.Api.Authorization;
using WebDocker.Api.Dtos;
using WebDocker.Data;
using WebDocker.Services;

namespace WebDocker.Api.Controllers;

[ApiController]
[Route("api/servers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ServerController : ControllerBase
{
    private readonly ServerService _serverService;
    private readonly DockerService _dockerService;
    private readonly ILogger<ServerController> _logger;

    public ServerController(ServerService serverService, DockerService dockerService, ILogger<ServerController> logger)
    {
        _serverService = serverService;
        _dockerService = dockerService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission("servers.read")]
    public async Task<ActionResult<IEnumerable<DockerServerDto>>> GetAll()
    {
        var userId = GetUserId();
        var servers = await _serverService.GetServers(userId);
        return Ok(servers.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    [RequirePermission("servers.read")]
    public async Task<ActionResult<DockerServerDto>> GetById(int id)
    {
        var userId = GetUserId();
        var server = await _serverService.GetServerById(id, userId);
        if (server == null)
        {
            return NotFound();
        }
        return Ok(ToDto(server));
    }

    [HttpPost]
    [RequirePermission("servers.write")]
    public async Task<ActionResult<DockerServerDto>> Create(CreateServerRequestDto request)
    {
        var userId = GetUserId();
        var server = new DockerServer
        {
            Name = request.Name,
            Host = request.Host,
            Port = request.Port,
            OperatingSystem = request.OperatingSystem,
            UseTls = request.UseTls,
            UserId = userId
        };
        var created = await _serverService.AddServer(server);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id:int}")]
    [RequirePermission("servers.write")]
    public async Task<ActionResult<DockerServerDto>> Update(int id, UpdateServerRequestDto request)
    {
        var userId = GetUserId();
        var server = await _serverService.GetServerById(id, userId);
        if (server == null)
        {
            return NotFound();
        }

        server.Name = request.Name;
        server.Host = request.Host;
        server.Port = request.Port;
        server.OperatingSystem = request.OperatingSystem;
        server.UseTls = request.UseTls;

        await _serverService.UpdateServer(server);
        return Ok(ToDto(server));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("servers.write")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var server = await _serverService.GetServerById(id, userId);
        if (server == null)
        {
            return NotFound();
        }
        await _serverService.DeleteServer(id, userId);
        return NoContent();
    }

    [HttpPost("{id:int}/test-connection")]
    [RequirePermission("servers.read")]
    public async Task<ActionResult<TestConnectionResponseDto>> TestConnection(int id)
    {
        var userId = GetUserId();
        var server = await _serverService.GetServerById(id, userId);
        if (server == null)
        {
            return NotFound();
        }
        var (success, message) = await _dockerService.TestConnection(server);
        return Ok(new TestConnectionResponseDto { Success = success, Message = message });
    }



    private string GetUserId()
    {
        string userId = null!;

        userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("No se encontró el id del usuario en el token");

        return userId;
    }

    private static DockerServerDto ToDto(DockerServer server)
    {
        return new DockerServerDto
        {
            Id = server.Id,
            Name = server.Name,
            Host = server.Host,
            Port = server.Port,
            OperatingSystem = server.OperatingSystem,
            UseTls = server.UseTls,
            CreatedAt = server.CreatedAt
        };
    }
}
