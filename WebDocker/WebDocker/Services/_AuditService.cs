using WebDocker.Data;

namespace WebDocker.Services;

// Servicio sencillo para dejar rastro de las acciones sensibles en la tabla AuditLogs.
public class _AuditService
{
    private readonly AppDbContext _db;

    public _AuditService(AppDbContext db) => _db = db;

    // Guarda una línea de auditoría. Llamar tras una operación sensible.
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
