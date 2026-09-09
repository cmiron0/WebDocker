using Azure.Core;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FluentUI.AspNetCore.Components;
using System.ComponentModel;
using System.Security.Claims;
using WebDocker.Api.Authorization;
using WebDocker.Api.Dtos;
using WebDocker.Data;
using WebDocker.Services;

namespace WebDocker.Api.Controllers;

[ApiController]
[Route("api/servers/{serverId:int}")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DockerServersController : ControllerBase
{
    private readonly ServerService _serverService;
    private readonly DockerService _dockerService;
    private readonly ILogger<DockerServersController> _logger;
    private readonly AuditLogService _auditLog;

    public DockerServersController(ServerService serverService, DockerService dockerService, ILogger<DockerServersController> logger, AuditLogService auditLog)
    {
        _serverService = serverService;
        _dockerService = dockerService;
        _logger = logger;
        _auditLog = auditLog;
    }

    [HttpGet("info")]
    [RequirePermission("containers.read")]
    public async Task<ActionResult<DockerEngineInfo>> GetInfo(int serverId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var info = await _dockerService.GetDockerInfo(server);
        await _auditLog.AppLog(User.Identity?.Name, "GetInfo", $"server:{serverId}, container:{0}", true, "");

        if (info == null)
        {
            await _auditLog.AppLog(User.Identity?.Name, "GetInfo", $"server:{serverId}, container:{0}", false, "No se pudo conectar con el Docker Engine");
            return StatusCode(503, new { message = "No se pudo conectar con el Docker Engine" });
        }
        return Ok(info);
    }

    [HttpGet("containers")]
    [RequirePermission("containers.read")]
    public async Task<ActionResult<IEnumerable<ContainerDto>>> GetContainers(int serverId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var containers = await _dockerService.GetContainers(server);

        await _auditLog.AppLog(User.Identity?.Name, "GetContainers", $"server:{serverId}, container:{0}", true, "");

        var result = containers.Select(ToDto);
        return Ok(result);
    }

    [HttpGet("containers/{containerId}")]
    [RequirePermission("containers.read")]
    public async Task<ActionResult<ContainerDetailDto>> GetContainerDetails(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var details = await _dockerService.GetContainerDetails(server, containerId);
        if (details == null)
        {
            await _auditLog.AppLog(User.Identity?.Name, "GetContainerDetails", $"server:{serverId}, container:{containerId}", false, "NotFound");
            return NotFound();
        }

        // Comando de arranque (lista de palabras).
        var command = new List<string>();
        if (details.Config != null && details.Config.Cmd != null)
        {
            command.AddRange(details.Config.Cmd);
        }

        // Variables de entorno.
        var environment = new List<string>();
        if (details.Config != null && details.Config.Env != null)
        {
            environment.AddRange(details.Config.Env);
        }

        // Puertos, con formato "80/tcp -> 0.0.0.0:8080".
        var ports = new List<string>();
        if (details.NetworkSettings != null && details.NetworkSettings.Ports != null)
        {
            foreach (var port in details.NetworkSettings.Ports)
            {
                var bindings = new List<string>();
                if (port.Value != null)
                {
                    foreach (var binding in port.Value)
                    {
                        bindings.Add($"{binding.HostIP}:{binding.HostPort}");
                    }
                }
                ports.Add($"{port.Key} → {string.Join(", ", bindings)}");
            }
        }

        await _auditLog.AppLog(User.Identity?.Name, "GetContainerDetails", $"server:{serverId}, container:{containerId}", true, "");

        var dto = new ContainerDetailDto
        {
            Id = details.ID ?? string.Empty,
            Name = details.Name?.TrimStart('/') ?? string.Empty,
            Image = details.Config?.Image ?? string.Empty,
            Status = details.State?.Status ?? string.Empty,
            Created = details.Created,
            StartedAt = details.State?.StartedAt ?? string.Empty,
            Platform = details.Platform ?? string.Empty,
            Hostname = details.Config?.Hostname ?? string.Empty,
            WorkingDir = details.Config?.WorkingDir ?? string.Empty,
            Command = command,
            Environment = environment,
            Ports = ports
        };
        return Ok(dto);
    }

    [HttpPost("containers")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> CreateContainer(int serverId, CreateContainerRequestDto request)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var parameters = new CreateContainerParameters
        {
            Image = request.Image,
            Name = request.Name,
            Env = request.Environment?.ToList(),
            Cmd = request.Command?.ToList()
        };

        if (request.PortBindings != null && request.PortBindings.Any())
        {
            parameters.HostConfig = new HostConfig
            {
                PortBindings = request.PortBindings.ToDictionary(
                    p => p.Key,
                    p => (IList<PortBinding>)new List<PortBinding> { new() { HostPort = p.Value } })
            };
            parameters.ExposedPorts = request.PortBindings.ToDictionary(
                p => p.Key,
                p => new EmptyStruct());
        }

        var (success, message, id) = await _dockerService.CreateContainer(server, parameters);

        await _auditLog.AppLog(User.Identity?.Name, "CreateContainer",$"server:{serverId}, image:{request.Image}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = id };
        return Ok(result);
    }

    [HttpPost("containers/{containerId}/start")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> StartContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.StartContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "StartContainer", $"server:{serverId}, container:{containerId}", success, message);
        
        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpPost("containers/{containerId}/stop")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> StopContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.StopContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "StopContainer", $"server:{serverId}, container:{containerId}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpPost("containers/{containerId}/restart")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> RestartContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.RestartContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "RestartContainer", $"server:{serverId}, container:{containerId}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpPost("containers/{containerId}/pause")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> PauseContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.PauseContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "PauseContainer", $"server:{serverId}, container:{containerId}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpPost("containers/{containerId}/unpause")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> UnpauseContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.UnPauseContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "UnpauseContainer", $"server:{serverId}, container:{containerId}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpDelete("containers/{containerId}")]
    [RequirePermission("containers.write")]
    public async Task<ActionResult<ContainerOperationResponseDto>> RemoveContainer(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var (success, message) = await _dockerService.RemoveContainer(server, containerId);
        await _auditLog.AppLog(User.Identity?.Name, "RemoveContainer", $"server:{serverId}, container:{containerId}", success, message);

        var result = new ContainerOperationResponseDto { Success = success, Message = message, ContainerId = containerId };
        return Ok(result);
    }

    [HttpGet("containers/{containerId}/logs")]
    [RequirePermission("containers.read")]
    public async Task<ActionResult<LogsResponseDto>> GetContainerLogs(int serverId, string containerId, int tail = 200)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        var logs = await _dockerService.GetContainerLogs(server, containerId, tail);
        await _auditLog.AppLog(User.Identity?.Name, "GetContainerLogs", $"server:{serverId}, container:{containerId}", true, "");

        return Ok(new LogsResponseDto { Logs = logs });
    }

    [HttpGet("containers/{containerId}/stats")]
    [RequirePermission("containers.read")]
    public async Task<ActionResult<ContainerStatsDto>> GetContainerStats(int serverId, string containerId)
    {
        var server = await GetServer(serverId);
        if (server == null) return NotFound();

        // Muestra puntual de CPU/memoria.
        var stats = await _dockerService.GetContainerStats(server, containerId);
        if (stats == null)
        {
            await _auditLog.AppLog(User.Identity?.Name, "GetContainerStats", $"server:{serverId}, container:{containerId}", false, "NotFound");
            return NotFound();
        }
        await _auditLog.AppLog(User.Identity?.Name, "GetContainerStats", $"server:{serverId}, container:{containerId}", true, "");

        var result = new ContainerStatsDto
        {
            CpuPercent = stats.CpuPercent,
            MemoryUsageMb = stats.MemoryUsageMb,
            MemoryLimitMb = stats.MemoryLimitMb,
            MemoryPercent = stats.MemoryPercent
        };
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

    private static ContainerDto ToDto(ContainerListResponse container)
    {

        var result = new ContainerDto
        {
            Id = container.ID,
            Name = container.Names != null && container.Names.Count > 0
                ? container.Names[0].TrimStart('/')
                : string.Empty,
            Image = container.Image,
            State = container.State,
            Status = container.Status,
            Created = container.Created,
            Ports = container.Ports?
                .Where(p => p.PublicPort > 0)
                .Select(p => $"{p.PublicPort}:{p.PrivatePort}/{p.Type}")
                .ToList() ?? new List<string>()

        };
        
        return result;
    }
}
