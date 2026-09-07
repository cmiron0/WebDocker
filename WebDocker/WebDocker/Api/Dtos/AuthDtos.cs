using System.ComponentModel.DataAnnotations;

namespace WebDocker.Api.Dtos;

public class LoginRequestDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public IList<string> Permissions { get; set; } = new List<string>();
    public DateTime ExpiresAt { get; set; }
}

public class UserInfoResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public IList<string> Permissions { get; set; } = new List<string>();

}

public class RefreshRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    public string Password { get; set; } = string.Empty;
}


