# Empaquetado Docker — WDockerClient

Imagen Docker del cliente Blazor WebAssembly. Se compila con el SDK de .NET y se
sirve el resultado (ficheros estáticos) con **nginx sobre Ubuntu 22.04**.

## Ficheros de esta carpeta
- `Dockerfile` — build multi-etapa (el SDK de .NET publica → Ubuntu + nginx sirve).
- `nginx/default.conf` — configuración de nginx: enrutado SPA, tipo MIME de `.wasm`, `gzip_static`.
- `Dockerfile.dockerignore` — excluye `bin/` y `obj/` del contexto de build.

## Construir la imagen
El contexto de build es la **raíz del proyecto** (`WDockerClient/`, donde está
`WDockerClient.csproj`), porque ahí está el código fuente. Por eso se apunta al
Dockerfile con `-f`:

```bash
# ejecutar desde la carpeta WDockerClient/
docker build -f Docker/Dockerfile -t wdockerclient .
```

## Ejecutar el contenedor
```bash
docker run -d -p 8080:80 --name wdockerclient wdockerclient
```

`-p 8080:80` mapea el puerto **8080 del host** (que es el origen CORS
`http://51.170.39.109:8080`) al puerto **80 de nginx** dentro del contenedor.

Abrir en el navegador: `http://<host>:8080`

## Configuración
- **URL de la API**: en `wwwroot/appsettings.json` (clave `ApiBaseUrl`). En la imagen
  apunta a `https://webdocker.azurewebsites.net/`. Se puede cambiar dentro del
  contenedor editando ese fichero, **sin recompilar**.
- **CORS**: la API WebDocker debe permitir el origen del cliente
  (`http://51.170.39.109:8080`), ya configurado en su `appsettings.json`.

## Estado / resultados
- `dotnet publish -c Release` genera correctamente `wwwroot/` (index.html, `_framework`,
  css, lib) — validado en local.
- La imagen **aún no se ha construido** porque la máquina de desarrollo no tiene Docker
  instalado. Ejecutar los comandos de arriba donde haya Docker (la máquina OCI o Docker Desktop).
- Para que el login funcione end-to-end hace falta la API WebDocker desplegada en Azure
  y accesible.
