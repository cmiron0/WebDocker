using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Docker.DotNet;
using Docker.DotNet.Models;
using Docker.DotNet.X509;
using WebDocker.Data;

namespace WebDocker.Services;

public class _DockerEngineService
{
    // Crea el cliente Docker para un servidor. Si el servidor usa TLS (puerto 2376) y tiene
    // certificados guardados, monta una conexión TLS mutua (cert de cliente + validación de CA).
    private DockerClient CreateClient(DockerServer server)
    {
        var scheme = server.UseTls ? "https" : "http";
        var uri = new Uri($"{scheme}://{server.Host}:{server.Port}");

        if (server.UseTls
            && !string.IsNullOrWhiteSpace(server.ClientCertPem)
            && !string.IsNullOrWhiteSpace(server.ClientKeyPem))
        {
            // Validación del certificado del servidor contra la CA indicada (si se dio).
            RemoteCertificateValidationCallback? validateServer = null;
            if (!string.IsNullOrWhiteSpace(server.CaCertPem))
            {
                var caCert = X509Certificate2.CreateFromPem(server.CaCertPem);
                validateServer = (_, cert, _, _) => ServerCertTrustedByCa(cert, caCert);
            }

            var credentials = new CertificateCredentials(LoadClientCertificate(server))
            {
                ServerCertificateValidationCallback = validateServer
            };
            return new DockerClientConfiguration(uri, credentials).CreateClient();
        }

        return new DockerClientConfiguration(uri).CreateClient();
    }

    // Construye el certificado de cliente (cert + clave privada) a partir del contenido PEM.

    // Problema en Azure Plan Free F1, al gestionar los certificados.
    //private static X509Certificate2 LoadClientCertificate(DockerServer server)
    //{
    //    // En Windows, un certificado creado desde PEM no sirve tal cual para el handshake TLS
    //    // se reimporta como PKCS#12 para que la clave privada sea utilizable.
    //    var cert = X509Certificate2.CreateFromPem(server.ClientCertPem, server.ClientKeyPem);
    //    return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pkcs12), null);
    //}

    private static X509Certificate2 LoadClientCertificate(DockerServer server)
    {
        // En Windows, un certificado creado desde PEM no sirve tal cual para el handshake TLS;
        // se reimporta como PKCS#12 para que la clave privada sea utilizable.

        // MachineKeySet: la clave privada se guarda en el almacén de claves de la MÁQUINA,
        // que NO depende del perfil de usuario. En Azure App Service el perfil de usuario no
        // se carga (por eso la carga por defecto, que usa el almacén del usuario, falla con
        // "The system cannot find the file specified"). El almacén de máquina sí existe y la
        // clave persistida sí es utilizable en el handshake TLS de Windows. En local funciona igual.
        var cert = X509Certificate2.CreateFromPem(server.ClientCertPem, server.ClientKeyPem);
        return X509CertificateLoader.LoadPkcs12(
            cert.Export(X509ContentType.Pkcs12),
            null,
            X509KeyStorageFlags.MachineKeySet);
    }

    // Comprueba que el certificado del servidor está firmado por la CA indicada.
    private static bool ServerCertTrustedByCa(X509Certificate? serverCert, X509Certificate2 caCert)
    {
        if (serverCert is not X509Certificate2 cert) return false;

        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(caCert);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        return chain.Build(cert);
    }

    /// <summary>
    /// Verifica si el servidor Docker es accesible.
    /// </summary>
    public async Task<(bool Success, string Message)> TestConnectionAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await client.System.PingAsync(cts.Token);
            return (true, "Conexión exitosa");
        }
        catch (Exception ex)
        {
            return (false, $"Error de conexión: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene información del Docker Engine (versión, OS, contenedores, imágenes).
    /// </summary>
    public async Task<DockerEngineInfo?> GetDockerInfoAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            var info = await client.System.GetSystemInfoAsync(cts.Token);
            var version = await client.System.GetVersionAsync(cts.Token);

            return new DockerEngineInfo
            {
                DockerVersion = version.Version,
                ApiVersion = version.APIVersion,
                OSType = info.OSType,
                OperatingSystem = info.OperatingSystem,
                Architecture = info.Architecture,
                TotalContainers = info.Containers,
                RunningContainers = info.ContainersRunning,
                StoppedContainers = info.ContainersStopped,
                PausedContainers = info.ContainersPaused,
                TotalImages = info.Images,
                MemoryTotal = info.MemTotal,
                CPUs = info.NCPU,
                ServerName = info.Name
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Lista todos los contenedores (en ejecución y detenidos).
    /// </summary>
    public async Task<IList<ContainerListResponse>> GetContainersAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await client.Containers.ListContainersAsync(new ContainersListParameters { All = true }, cts.Token);
        }
        catch
        {
            return new List<ContainerListResponse>();
        }
    }

    /// <summary>
    /// Obtiene los detalles de un contenedor específico.
    /// </summary>
    public async Task<ContainerInspectResponse?> GetContainerDetailsAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await client.Containers.InspectContainerAsync(containerId, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Crea un nuevo contenedor.
    /// </summary>
    public async Task<(bool Success, string Message, string? ContainerId)> CreateContainerAsync(
        DockerServer server, CreateContainerParameters parameters)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Intentar descargar la imagen primero
            try
            {
                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = parameters.Image },
                    null,
                    new Progress<JSONMessage>(),
                    cts.Token);
            }
            catch
            {
                // La imagen podría ya existir localmente
            }

            var response = await client.Containers.CreateContainerAsync(parameters, cts.Token);
            return (true, "Contenedor creado correctamente", response.ID);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear contenedor: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Inicia un contenedor.
    /// </summary>
    public async Task<(bool Success, string Message)> StartContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var started = await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters(), cts.Token);
            return started
                ? (true, "Contenedor iniciado")
                : (true, "El contenedor ya estaba en ejecución");
        }
        catch (Exception ex)
        {
            return (false, $"Error al iniciar: {ex.Message}");
        }
    }

    /// <summary>
    /// Detiene un contenedor.
    /// </summary>
    public async Task<(bool Success, string Message)> StopContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var stopped = await client.Containers.StopContainerAsync(containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 10 }, cts.Token);
            return stopped
                ? (true, "Contenedor detenido")
                : (true, "El contenedor ya estaba detenido");
        }
        catch (Exception ex)
        {
            return (false, $"Error al detener: {ex.Message}");
        }
    }

    /// <summary>
    /// Reinicia un contenedor.
    /// </summary>
    public async Task<(bool Success, string Message)> RestartContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await client.Containers.RestartContainerAsync(containerId, new ContainerRestartParameters { WaitBeforeKillSeconds = 10 }, cts.Token);
            return (true, "Contenedor reiniciado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al reiniciar: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un contenedor (forzando la detención si es necesario).
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters { Force = true }, cts.Token);
            return (true, "Contenedor eliminado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar: {ex.Message}");
        }
    }

    /// <summary>
    /// Renombra un contenedor.
    /// </summary>
    public async Task<(bool Success, string Message)> RenameContainerAsync(DockerServer server, string containerId, string newName)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Containers.RenameContainerAsync(containerId, new ContainerRenameParameters { NewName = newName }, cts.Token);
            return (true, "Contenedor renombrado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al renombrar: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista las imágenes disponibles en el servidor.
    /// </summary>
    public async Task<IList<ImagesListResponse>> GetImagesAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await client.Images.ListImagesAsync(new ImagesListParameters { All = false }, cts.Token);
        }
        catch
        {
            return new List<ImagesListResponse>();
        }
    }

    public async Task<(bool Success, string Message)> PullImageAsync(DockerServer server, string imageName)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = imageName },
                null,
                new Progress<JSONMessage>(),
                cts.Token);
            return (true, $"Imagen {imageName} descargada");
        }
        catch (Exception ex)
        {
            return (false, $"Error al descargar imagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Carga una imagen desde un fichero .tar (equivalente a 'docker load').
    /// El .tar debe haberse generado con 'docker save'. La imagen queda registrada
    /// en el almacén del host Docker remoto y aparece en el listado de imágenes.
    /// </summary>
    public async Task<(bool Success, string Message)> LoadImageFromTarAsync(DockerServer server, Stream tarStream)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
            await client.Images.LoadImageAsync(
                new ImageLoadParameters(),
                tarStream,
                new Progress<JSONMessage>(),
                cts.Token);
            return (true, "Imagen cargada correctamente desde el fichero .tar");
        }
        catch (Exception ex)
        {
            return (false, $"Error al cargar la imagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Eliminar imagen.
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveImageAsync(DockerServer server, string imageId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await client.Images.DeleteImageAsync(imageId,
                new ImageDeleteParameters { Force = true }, cts.Token);
            return (true, "Imagen eliminada");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar imagen: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista de volumenes.
    /// </summary>
    public async Task<IList<VolumeResponse>> GetVolumesAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await client.Volumes.ListAsync(cts.Token);
            return response.Volumes ?? new List<VolumeResponse>();
        }
        catch
        {
            return new List<VolumeResponse>();
        }
    }

    /// <summary>
    /// Crear un volumen.
    /// </summary>
    public async Task<(bool Success, string Message)> CreateVolumeAsync(DockerServer server, string name)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Volumes.CreateAsync(new VolumesCreateParameters { Name = name }, cts.Token);
            return (true, $"Volumen {name} creado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear volumen: {ex.Message}");
        }
    }

    /// <summary>
    /// Eliminar un volumen.
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveVolumeAsync(DockerServer server, string name)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Volumes.RemoveAsync(name, force: true, cts.Token);
            return (true, "Volumen eliminado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar volumen: {ex.Message}");
        }
    }

    /// <summary>
    /// Lista de redes.
    /// </summary>
    public async Task<IList<NetworkResponse>> GetNetworksAsync(DockerServer server)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await client.Networks.ListNetworksAsync(new NetworksListParameters(), cts.Token);
        }
        catch
        {
            return new List<NetworkResponse>();
        }
    }

    /// <summary>
    /// Crear una red.
    /// </summary>
    public async Task<(bool Success, string Message, string? NetworkId)> CreateNetworkAsync(DockerServer server, string name, string driver)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await client.Networks.CreateNetworkAsync(
                new NetworksCreateParameters { Name = name, Driver = driver }, cts.Token);
            return (true, $"Red {name} creada", response.ID);
        }
        catch (Exception ex)
        {
            return (false, $"Error al crear red: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Eliminar un red.
    /// </summary>
    public async Task<(bool Success, string Message)> RemoveNetworkAsync(DockerServer server, string networkId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Networks.DeleteNetworkAsync(networkId, cts.Token);
            return (true, "Red eliminada");
        }
        catch (Exception ex)
        {
            return (false, $"Error al eliminar red: {ex.Message}");
        }
    }

    /// <summary>
    /// Pausa un contenedor (congela sus procesos sin pararlo del todo).
    /// </summary>
    public async Task<(bool Success, string Message)> PauseContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Containers.PauseContainerAsync(containerId, cts.Token);
            return (true, "Contenedor pausado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al pausar: {ex.Message}");
        }
    }

    /// <summary>
    /// Reanuda un contenedor previamente pausado.
    /// </summary>
    public async Task<(bool Success, string Message)> UnpauseContainerAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await client.Containers.UnpauseContainerAsync(containerId, cts.Token);
            return (true, "Contenedor reanudado");
        }
        catch (Exception ex)
        {
            return (false, $"Error al reanudar: {ex.Message}");
        }
    }

    /// <summary>
    /// Log de un contenedor
    /// </summary>
    public async Task<string> GetContainerLogsAsync(DockerServer server, string containerId, int tail = 200)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Tail = tail.ToString(),
                Timestamps = false
            };
            using var stream = await client.Containers.GetContainerLogsAsync(containerId, false, parameters, cts.Token);
            var (stdout, stderr) = await stream.ReadOutputToEndAsync(cts.Token);
            return stdout + stderr;
        }
        catch (Exception ex)
        {
            return $"Error al obtener logs: {ex.Message}";
        }
    }

    /// <summary>
    /// Muestra de estadísticas (CPU y memoria) de un contenedor y calcula los porcentajes.
    /// </summary>
    public async Task<ContainerStats?> GetContainerStatsAsync(DockerServer server, string containerId)
    {
        try
        {
            using var client = CreateClient(server);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            ContainerStatsResponse? sample = null;

            // Con Stream=false recibimos una sola muestra (incluye datos previos 'precpu',
            // necesarios para calcular el % de CPU). Docker.DotNet la entrega por IProgress.
            await client.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = false },
                new Progress<ContainerStatsResponse>(s => sample = s),
                cts.Token);

            if (sample == null) return null;

            // Fórmula oficial de Docker para el % de CPU
            double cpuDelta = sample.CPUStats.CPUUsage.TotalUsage - sample.PreCPUStats.CPUUsage.TotalUsage;
            double systemDelta = sample.CPUStats.SystemUsage - sample.PreCPUStats.SystemUsage;
            double cpuCount = sample.CPUStats.CPUUsage.PercpuUsage?.Count ?? (int)sample.CPUStats.OnlineCPUs;
            if (cpuCount == 0) cpuCount = 1;

            double cpuPercent = 0;
            if (systemDelta > 0 && cpuDelta > 0)
            {
                cpuPercent = (cpuDelta / systemDelta) * cpuCount * 100.0;
            }

            // Memoria (de bytes a MB)
            double usageMb = sample.MemoryStats.Usage / (1024.0 * 1024.0);
            double limitMb = sample.MemoryStats.Limit / (1024.0 * 1024.0);
            double memPercent = limitMb > 0 ? (usageMb / limitMb) * 100.0 : 0;

            return new ContainerStats
            {
                CpuPercent = Math.Round(cpuPercent, 2),
                MemoryUsageMb = Math.Round(usageMb, 1),
                MemoryLimitMb = Math.Round(limitMb, 1),
                MemoryPercent = Math.Round(memPercent, 2)
            };
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// DTO estadísticas de uso de un contenedor.
/// </summary>
public class ContainerStats
{
    public double CpuPercent { get; set; }       // % de CPU usado
    public double MemoryUsageMb { get; set; }    // memoria usada (MB)
    public double MemoryLimitMb { get; set; }    // límite de memoria (MB)
    public double MemoryPercent { get; set; }    // % de memoria usada
}

/// <summary>
/// DTO información del Docker Engine.
/// </summary>
public class DockerEngineInfo
{
    public string DockerVersion { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string OSType { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Architecture { get; set; } = string.Empty;
    public long TotalContainers { get; set; }
    public long RunningContainers { get; set; }
    public long StoppedContainers { get; set; }
    public long PausedContainers { get; set; }
    public long TotalImages { get; set; }
    public long MemoryTotal { get; set; }
    public long CPUs { get; set; }
    public string ServerName { get; set; } = string.Empty;

    public string MemoryFormatted => MemoryTotal > 0 ? $"{MemoryTotal / (1024 * 1024 * 1024.0):F1} GB" : "N/A";
}
