using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebDocker.Data
{
    public class DockerServer
    {
        [Key]
        public int Id { get; set; }

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

        // Certificados TLS (contenido PEM) para conectar por el puerto 2376 con TLS mutuo.
        // Se guardan en la BD (como texto) para que funcione igual en desarrollo y en Azure.
        public string? CaCertPem { get; set; }        // ca.pem  (CA - Certificate Authorityque que valida el certificado del servidor)
        public string? ClientCertPem { get; set; }    // cert.pem (certificado de cliente)
        public string? ClientKeyPem { get; set; }     // key.pem  (clave privada del cliente)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public AppUser? User { get; set; }
    }
}
