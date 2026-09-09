using System.Net.Http.Headers;
using System.Net.Http.Json;
using WDockerClient.Models;

namespace WDockerClient.Services;

// Envuelve todas las llamadas a la API REST de WebDocker y guarda el token JWT
// en memoria. Al ser un servicio con ámbito (scoped) en WebAssembly, hay una
// única instancia durante toda la sesión, así que el token se conserva mientras
// se navega. Recargar la página con F5 pierde la sesión (token en memoria).
public class ApiService
{
    private readonly HttpClient _http;
    private List<string> _permissions = new List<string>();

    public ApiService(HttpClient http) => _http = http;

    public string? UserName { get; private set; }
    public bool IsAuthenticated { get; private set; }

    // Comprueba un permiso RBAC (la API nos lo devuelve en el login) para
    // deshabilitar botones. La API vuelve a validar el permiso en cada llamada.
    public bool HasPermission(string code) => _permissions.Contains(code);

    // Devuelve null si el login es correcto, o un mensaje de error si falla.
    public async Task<string?> Login(string userName, string password)
    {
        try
        {
            var request = new WDCLoginRequest { UserName = userName, Password = password };
            var response = await _http.PostAsJsonAsync("api/auth/login", request);

            if (!response.IsSuccessStatusCode)
            {
                return "Usuario o contraseña incorrectos.";
            }

            var data = await response.Content.ReadFromJsonAsync<WDCLoginResponse>();
            if (data == null || string.IsNullOrEmpty(data.Token))
            {
                return "El servidor no devolvió un token válido.";
            }

            UserName = data.UserName;
            IsAuthenticated = true;
            _permissions = data.Permissions;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", data.Token);
            return null;
        }
        catch
        {
            return "No se pudo conectar con la API de WebDocker.";
        }
    }

    public void Logout()
    {
        UserName = null;
        IsAuthenticated = false;
        _permissions = new List<string>();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------
    // Servers
    // ----------------------------------------------------------------------------------------------------------------------------------------------
    public async Task<List<WDCDockerServer>> GetServers()
    {
        var servers = await _http.GetFromJsonAsync<List<WDCDockerServer>>("api/servers");
        if (servers == null)
        {
            return new List<WDCDockerServer>();
        }
        return servers;
    }

    public async Task<WDCDockerServer?> GetServer(int serverId)
    {
        return await _http.GetFromJsonAsync<WDCDockerServer>($"api/servers/{serverId}");
    }

    // ----------------------------------------------------------------------------------------------------------------------------------------------
    // Container
    // ----------------------------------------------------------------------------------------------------------------------------------------------
    public async Task<List<WDCContainer>> GetContainers(int serverId)
    {
        var containers = await _http.GetFromJsonAsync<List<WDCContainer>>($"api/servers/{serverId}/containers");
        if (containers == null)
        {
            return new List<WDCContainer>();
        }
        return containers;
    }

    public async Task<WDCContainerOperationResponse?> StartContainer(int serverId, string containerId)
    {
        return await PostOperation($"api/servers/{serverId}/containers/{containerId}/start");
    }

    public async Task<WDCContainerOperationResponse?> StopContainer(int serverId, string containerId)
    {
        return await PostOperation($"api/servers/{serverId}/containers/{containerId}/stop");
    }

    public async Task<WDCContainerOperationResponse?> RestartContainer(int serverId, string containerId)
    {
        return await PostOperation($"api/servers/{serverId}/containers/{containerId}/restart");
    }

    public async Task<WDCContainerOperationResponse?> PauseContainer(int serverId, string containerId)
    {
        return await PostOperation($"api/servers/{serverId}/containers/{containerId}/pause");
    }

    public async Task<WDCContainerOperationResponse?> UnpauseContainer(int serverId, string containerId)
    {
        return await PostOperation($"api/servers/{serverId}/containers/{containerId}/unpause");
    }

    public async Task<WDCContainerOperationResponse?> RemoveContainer(int serverId, string containerId)
    {
        var response = await _http.DeleteAsync($"api/servers/{serverId}/containers/{containerId}");
        return await response.Content.ReadFromJsonAsync<WDCContainerOperationResponse>();
    }

    public async Task<WDCContainerOperationResponse?> CreateContainer(int serverId, WDCContainerRequest request)
    {
        var response = await _http.PostAsJsonAsync($"api/servers/{serverId}/containers", request);
        return await response.Content.ReadFromJsonAsync<WDCContainerOperationResponse>();
    }

    public async Task<string> GetLogs(int serverId, string containerId)
    {
        var data = await _http.GetFromJsonAsync<WDCLogsResponse>(
            $"api/servers/{serverId}/containers/{containerId}/logs?tail=200");
        if (data == null)
        {
            return "";
        }
        return data.Logs;
    }

    // Muestra puntual de CPU/memoria.
    public async Task<WDCContainerStats?> GetStats(int serverId, string containerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<WDCContainerStats>(
                $"api/servers/{serverId}/containers/{containerId}/stats");
        }
        catch
        {
            return null;
        }
    }

    // Detalle de un contenedor.
    public async Task<WDCContainerDetail?> GetContainerDetail(int serverId, string containerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<WDCContainerDetail>(
                $"api/servers/{serverId}/containers/{containerId}");
        }
        catch
        {
            return null;
        }
    }

    private async Task<WDCContainerOperationResponse?> PostOperation(string url)
    {
        var response = await _http.PostAsync(url, null);
        return await response.Content.ReadFromJsonAsync<WDCContainerOperationResponse>();
    }
}
