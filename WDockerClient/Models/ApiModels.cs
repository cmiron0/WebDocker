namespace WDockerClient.Models;

// Modelos que reflejan los Modelos de la API REST de WebDocker.

public class WDCLoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class WDCLoginResponse
{
    public string Token { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public string UserName { get; set; } = "";
    public List<string> Roles { get; set; } = new List<string>();
    public List<string> Permissions { get; set; } = new List<string>();
    public DateTime ExpiresAt { get; set; }
}

public class WDCDockerServer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public string OperatingSystem { get; set; } = "";
    public bool UseTls { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WDCContainer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string State { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Created { get; set; }
    public List<string> Ports { get; set; } = new List<string>();
}

public class WDCContainerRequest
{
    public string Image { get; set; } = "";
    public string? Name { get; set; }
    public Dictionary<string, string>? PortBindings { get; set; }
    public List<string>? Environment { get; set; }
    public List<string>? Command { get; set; }
}

public class WDCContainerOperationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? ContainerId { get; set; }
}

public class WDCLogsResponse
{
    public string Logs { get; set; } = "";
}

public class WDCContainerStats
{
    public double CpuPercent { get; set; }
    public double MemoryUsageMb { get; set; }
    public double MemoryLimitMb { get; set; }
    public double MemoryPercent { get; set; }
}

public class WDCContainerDetail
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Image { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Created { get; set; }
    public string StartedAt { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string WorkingDir { get; set; } = "";
    public List<string> Command { get; set; } = new List<string>();
    public List<string> Environment { get; set; } = new List<string>();
    public List<string> Ports { get; set; } = new List<string>();
}
