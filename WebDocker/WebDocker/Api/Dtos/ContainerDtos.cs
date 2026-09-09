using System.ComponentModel.DataAnnotations;

namespace WebDocker.Api.Dtos;

public class ContainerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public IList<string> Ports { get; set; } = new List<string>();
}

public class CreateContainerRequestDto
{
    [Required]
    public string Image { get; set; } = string.Empty;

    public string? Name { get; set; }

    public IDictionary<string, string>? PortBindings { get; set; }

    public IList<string>? Environment { get; set; }

    public IList<string>? Command { get; set; }
}

public class ContainerOperationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ContainerId { get; set; }
}

public class LogsResponseDto
{
    public string Logs { get; set; } = string.Empty;
}

public class ContainerStatsDto
{
    public double CpuPercent { get; set; }       // % de CPU usado
    public double MemoryUsageMb { get; set; }    // memoria usada (MB)
    public double MemoryLimitMb { get; set; }    // límite de memoria (MB)
    public double MemoryPercent { get; set; }    // % de memoria usada
}

// Detalle de un contenedor
// Simplificación de ContainerInspectResponse de Docker.DotNet.
public class ContainerDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public string StartedAt { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string WorkingDir { get; set; } = string.Empty;
    public IList<string> Command { get; set; } = new List<string>();
    public IList<string> Environment { get; set; } = new List<string>();
    public IList<string> Ports { get; set; } = new List<string>();
}
