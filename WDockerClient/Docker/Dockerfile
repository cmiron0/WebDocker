
## Imagen Docker del cliente WDockerClient (Blazor WebAssembly).
## Se compila con el SDK de .NET (genera ficheros estáticos) y se sirven
## con nginx sobre Ubuntu.

## El contexto de build es la RAÍZ del proyecto (WDockerClient/), donde está el código fuente. 
## Construir desde WDockerClient:
##     docker build -f Docker/Dockerfile -t wdockerclient .
## -------------------------------------------------------------------------------------------------------------------------

# Etapa 1: compilar y publicar el cliente
# ------------------------------------------------------------------------

# Imagen base de la primera etapa (SDK .Net 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Carperta de trabajo
WORKDIR /src

# Copiar solo el .csproj y Restaurar
COPY WDockerClient.csproj ./

# Restaruar dependencias del proyecto
RUN dotnet restore

# Copiar el resto del código y la publicación en modo Release.
COPY . ./

# Generar publiación en Release
RUN dotnet publish -c Release -o /app/publish


# Etapa 2: servir los ficheros estáticos con nginx sobre Ubuntu
# ------------------------------------------------------------------------

# Imagen base de la segunda etapa
FROM ubuntu:22.04

# Instalar nginx y limpiar la cache de apt para reducir el tamaño de la imagen.
RUN apt-get update \
    && apt-get install -y nginx \
    && rm -rf /var/lib/apt/lists/*

# Copiar el sitio publicado. En Blazor WebAssembly la salida está en wwwroot.
COPY --from=build /app/publish/wwwroot /var/www/html

# Copiar nuestra configuración de nginx (enrutado SPA + tipo MIME de wasm).
COPY Docker/nginx/nginx.conf /etc/nginx/sites-available/default

# nginx escucha en el puerto 80 dentro del contenedor.
EXPOSE 80

# Arrancar nginx en primer plano para que el contenedor no se cierre.
CMD ["nginx", "-g", "daemon off;"]
