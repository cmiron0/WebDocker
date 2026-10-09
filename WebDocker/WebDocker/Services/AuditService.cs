using WebDocker.Data;

namespace WebDocker.Services;

// Servicio AuditLogs.
public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    // Guarda una línea de auditoría. 
    public async Task LogAsync(string? userName, string action, string? target, bool success, string? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserName = userName,
            Action = action,
            Target = target,
            Success = success,
            Details = details
        });
        await _db.SaveChangesAsync();
    }
}
