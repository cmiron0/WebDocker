using System.ComponentModel.DataAnnotations;

namespace WebDocker.Api.Dtos;

public class ImageDto
{
    public string Id { get; set; } = string.Empty;
    public IList<string> RepoTags { get; set; } = new List<string>();
    public long Size { get; set; }
    public DateTime Created { get; set; }
}

public class PullImageRequestDto
{
    [Required]
    public string Image { get; set; } = string.Empty;
}

public class VolumeDto
{
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Mountpoint { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CreateVolumeRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
}

public class NetworkDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public DateTime Created { get; set; }
}

public class CreateNetworkRequestDto
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string Driver { get; set; } = "bridge";
}

public class OperationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
