using System.ComponentModel.DataAnnotations;

namespace WebDocker.Data
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;
    }
}
