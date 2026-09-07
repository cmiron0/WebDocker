using System.ComponentModel.DataAnnotations;

namespace WebDocker.Data
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; } = false;
    }
}
