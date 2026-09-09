using Serilog;
using Serilog.Core;
using System.Runtime.CompilerServices;
using WebDocker.Data;

namespace WebDocker.Services;

// Servicio de Log, de las acciones realizadas (AuditLogs).
public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Guarda una línea de auditoría. Llamar tras una operación sensible.
    public async Task AppLog(string? userName, string action, string? target, bool success, string? details = null
        , [CallerMemberName] string callerMethod = "", [CallerFilePath] string callerFile = "")
    {
        // Los rellena el COMPILADOR en cada punto de llamada
        // [CallerMemberName] string callerMethod
        // [CallerFilePath] string callerFile

        var callerClass = Path.GetFileNameWithoutExtension(callerFile);

        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserName = userName,
                Action = action,
                Target = target,
                Success = success,
                Details = $"[{callerClass}.{callerMethod}] {details}"
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation($"Audit:{action} desde {callerClass}.{callerMethod}");
        }
        catch (Exception ex) 
        {
            _logger.LogInformation($"Audit:{action} desde {callerClass}.{callerMethod}: {ex.Message} ");
        }

    }
}
