using System.ComponentModel.DataAnnotations;

namespace WebDocker.Api.Dtos;

public class DockerServerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public bool UseTls { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateServerRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 2375;

    [Required]
    [MaxLength(20)]
    public string OperatingSystem { get; set; } = "Linux";

    public bool UseTls { get; set; } = false;
}

public class UpdateServerRequestDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    [Required]
    [MaxLength(20)]
    public string OperatingSystem { get; set; } = string.Empty;

    public bool UseTls { get; set; }
}

public class TestConnectionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
