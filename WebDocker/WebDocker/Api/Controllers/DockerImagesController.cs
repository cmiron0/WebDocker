using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;
using WebDocker.Api.Authorization;
using WebDocker.Api.Dtos;
using WebDocker.Data;
using WebDocker.Services;

namespace WebDocker.Api.Controllers;

[ApiController]
[Route("api/servers/{serverId:int}/images")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DockerImagesController : ControllerBase
{
    private readonly ServerService _serverService;
    private readonly DockerService _dockerService;
    private readonly ILogger<DockerImagesController> _logger;
    private readonly AuditLogService _auditLog;

    public DockerImagesController(ServerService serverService, DockerService dockerService, ILogger<DockerImagesController> logger, AuditLogService auditLog)
    {
        _serverService = serverService;
        _dockerService = dockerService;
        _logger = logger;
        _auditLog = auditLog; 
    }

    [HttpGet]
    [RequirePermission("images.read")]
    public async Task<ActionResult<IEnumerable<ImageDto>>> GetAll(int serverId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var images = await _dockerService.GetImages(server);
        await _auditLog.AppLog(User.Identity?.Name, "GetAll", $"server:{serverId}, Images", true, "");

        var result = images.Select(i => new ImageDto
        {
            Id = i.ID,
            RepoTags = i.RepoTags ?? new List<string>(),
            Size = i.Size,
            Created = i.Created
        });

        return Ok(result);
    }

    [HttpPost("pull")]
    [RequirePermission("images.write")]
    public async Task<ActionResult<OperationResponseDto>> Pull(int serverId, PullImageRequestDto request)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.PullImage(server, request.Image);
        await _auditLog.AppLog(User.Identity?.Name, "Pull", $"server:{serverId}, Image", success, message);

        var result = new OperationResponseDto { Success = success, Message = message };
        return Ok(result);
    }

    [HttpDelete("{imageId}")]
    [RequirePermission("images.write")]
    public async Task<ActionResult<OperationResponseDto>> Delete(int serverId, string imageId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.RemoveImage(server, imageId);
        await _auditLog.AppLog(User.Identity?.Name, "Delete", $"server:{serverId}, image:{imageId}", success, message);

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
