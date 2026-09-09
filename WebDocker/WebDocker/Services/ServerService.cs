using Microsoft.EntityFrameworkCore;
using WebDocker.Data;

namespace WebDocker.Services;

public class ServerService
{
    private readonly AppDbContext _db;

    public ServerService(AppDbContext db) => _db = db;


    // ----------------------------------------------------------------------------------------------------------------------------------------------
    // ----------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Lista todos los servidores.
    /// </summary>
    public async Task<List<DockerServer>> GetServers(string userId)
    {
        return await _db.DockerServers
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Obtener un servidor por id y usuario.
    /// </summary>
    public async Task<DockerServer?> GetServerById(int id, string userId)
    {
        return await _db.DockerServers.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    }

    /// <summary>
    /// Nuevo servidor.
    /// </summary>
    public async Task<DockerServer> AddServer(DockerServer server)
    {
        server.CreatedAt = DateTime.UtcNow;
        _db.DockerServers.Add(server);
        await _db.SaveChangesAsync();
        return server;
    }

    /// <summary>
    /// Actualizar servidor.
    /// </summary>
    public async Task UpdateServer(DockerServer server)
    {
        var dbServer = await _db.DockerServers.FindAsync(server.Id);
        if (dbServer == null) return;

        _db.Entry(dbServer).CurrentValues.SetValues(server);
        await _db.SaveChangesAsync();
    }


    /// <summary>
    /// Borrar servidor por id y usuario.
    /// </summary>
    public async Task DeleteServer(int id, string userId)
    {
        var server = await _db.DockerServers.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (server != null)
        {
            _db.DockerServers.Remove(server);
            await _db.SaveChangesAsync();
        }
    }
}
