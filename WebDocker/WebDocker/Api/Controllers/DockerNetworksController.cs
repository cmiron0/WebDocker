using Azure.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebDocker.Api.Authorization;
using WebDocker.Api.Dtos;
using WebDocker.Data;
using WebDocker.Services;

namespace WebDocker.Api.Controllers;

[ApiController]
[Route("api/servers/{serverId:int}/networks")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DockerNetworksController : ControllerBase
{
    private readonly ServerService _serverService;
    private readonly DockerService _dockerService;
    private readonly ILogger<DockerNetworksController> _logger;
    private readonly AuditLogService _auditLog;

    public DockerNetworksController(ServerService serverService, DockerService dockerService, ILogger<DockerNetworksController> logger, AuditLogService auditLog)
    {
        _serverService = serverService;
        _dockerService = dockerService;
        _logger = logger;
        _auditLog = auditLog;
    }

    [HttpGet]
    [RequirePermission("networks.read")]
    public async Task<ActionResult<IEnumerable<NetworkDto>>> GetAll(int serverId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var networks = await _dockerService.GetNetworks(server);
        await _auditLog.AppLog(User.Identity?.Name, "GetAll", $"server:{serverId}, Networks", true, "");

        var result = networks.Select(n => new NetworkDto
        {
            Id = n.ID,
            Name = n.Name,
            Driver = n.Driver,
            Scope = n.Scope,
            Created = n.Created
        });
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("networks.write")]
    public async Task<ActionResult<OperationResponseDto>> Create(int serverId, CreateNetworkRequestDto request)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message, _) = await _dockerService.CreateNetwork(server, request.Name, request.Driver);
        await _auditLog.AppLog(User.Identity?.Name, "Create", $"server:{serverId}, Network", success, message);

        var result = new OperationResponseDto { Success = success, Message = message };
        return Ok(result);
    }

    [HttpDelete("{networkId}")]
    [RequirePermission("networks.write")]
    public async Task<ActionResult<OperationResponseDto>> Delete(int serverId, string networkId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.RemoveNetwork(server, networkId);
        await _auditLog.AppLog(User.Identity?.Name, "Delete", $"server:{serverId}, Network:{networkId}", success, message);

        var result = new OperationResponseDto { Success = success, Message = message };
        return Ok(result);
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------------------------------------------------
    private async Task<DockerServer?> GetServer(int serverId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (userId == null) return null;

        var result = await _serverService.GetServerById(serverId, userId);
        return result;
    }
}
