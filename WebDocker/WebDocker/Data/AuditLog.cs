using System.ComponentModel.DataAnnotations;

namespace WebDocker.Data
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }
        public string? UserName { get; set; }
        [Required] public string Action { get; set; } = "";
        public string? Target { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
