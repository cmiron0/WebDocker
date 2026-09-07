using Docker.DotNet.Models;
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
[Route("api/servers/{serverId:int}/volumes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DockerVolumesController : ControllerBase
{
    private readonly ServerService _serverService;
    private readonly DockerService _dockerService;
    private readonly ILogger<DockerVolumesController> _logger;
    private readonly AuditLogService _auditLog;

    public DockerVolumesController(ServerService serverService, DockerService dockerService, ILogger<DockerVolumesController> logger, AuditLogService auditLog)
    {
        _serverService = serverService;
        _dockerService = dockerService;
        _logger = logger;
        _auditLog = auditLog;
    }

    [HttpGet]
    [RequirePermission("volumes.read")]
    public async Task<ActionResult<IEnumerable<VolumeDto>>> GetAll(int serverId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var volumes = await _dockerService.GetVolumes(server);
        await _auditLog.AppLog(User.Identity?.Name, "GetAll", $"server:{serverId}, Volumes", true, "");

        var result = volumes.Select(v => new VolumeDto
        {
            Name = v.Name,
            Driver = v.Driver,
            Mountpoint = v.Mountpoint,
            CreatedAt = v.CreatedAt
        });
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission("volumes.write")]
    public async Task<ActionResult<OperationResponseDto>> Create(int serverId, CreateVolumeRequestDto request)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.CreateVolume(server, request.Name);
        await _auditLog.AppLog(User.Identity?.Name, "Create", $"server:{serverId}, Volume", success, message);

        var result = new OperationResponseDto { Success = success, Message = message };
        return Ok(result);
    }

    [HttpDelete("{name}")]
    [RequirePermission("volumes.write")]
    public async Task<ActionResult<OperationResponseDto>> Delete(int serverId, string name)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.RemoveVolume(server, name);
        await _auditLog.AppLog(User.Identity?.Name, "Delete", $"server:{serverId}, Volume:{name}", success, message);

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
