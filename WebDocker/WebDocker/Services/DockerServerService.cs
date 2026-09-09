using Microsoft.EntityFrameworkCore;
using WebDocker.Data;

namespace WebDocker.Services;

public class DockerServerService
{
    private readonly AppDbContext _context;

    public DockerServerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<DockerServer>> GetServersAsync(string userId)
    {
        return await _context.DockerServers
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<DockerServer?> GetServerByIdAsync(int id, string userId)
    {
        return await _context.DockerServers.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
    }

    public async Task<DockerServer> AddServerAsync(DockerServer server)
    {
        server.CreatedAt = DateTime.UtcNow;
        _context.DockerServers.Add(server);
        await _context.SaveChangesAsync();
        return server;
    }

    public async Task UpdateServerAsync(DockerServer server)
    {
        // Buscamos la entidad que el contexto y le copiamos los valores.
        // (el DbContext vive durante todo el circuito de Blazor Server).
        var existing = await _context.DockerServers.FindAsync(server.Id);
        if (existing == null) return;

        _context.Entry(existing).CurrentValues.SetValues(server);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteServerAsync(int id, string userId)
    {
        var server = await _context.DockerServers.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (server != null)
        {
            _context.DockerServers.Remove(server);
            await _context.SaveChangesAsync();
        }
    }
}
