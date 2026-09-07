
## Para desplegar una aplicación Blazor WebAssembly en Nginx, necesitas un Dockerfile multi-etapa que compile el proyecto usando 
## el SDK de .NET y un archivo nginx.conf adaptado para que el enrutamiento interno de Blazor funcione correctamente 
## (evitando errores 404 al recargar páginas).

## 1. DockerfileEste fichero compila tu aplicación .NET (configurable para .NET 8.0, 9.0, etc.) 
##    y transfiere los activos web estáticos (wwwroot) al contenedor ligero de Nginx.dockerfile# 
## -------------------------------------------------------------------------------------------------------------------------

# Etapa 1: Compilación de la aplicación Blazor WebAssembly
# -----------------------------------------------------------------------
FROM ://microsoft.com AS builder
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias
COPY ["BlazorApp.csproj", "./"]
RUN dotnet restore "./BlazorApp.csproj"

# Copiar el resto del código y compilar en modo Release
COPY . .
RUN dotnet publish "BlazorApp.csproj" -c Release -o /app/publish

# Etapa 2: Servidor web Nginx para contenido estático
# ------------------------------------------------------------------------
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Limpiar los archivos por defecto de Nginx
RUN rm -rf ./*

# Copiar los archivos estáticos de Blazor publicados (HTML, JS, CSS, WASM)
COPY --from=builder /app/publish/wwwroot .

# Copiar la configuración personalizada de Nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]


## 2. nginx.conf
## Es imprescindible incluir este archivo en la misma raíz que el Dockerfile. Configura la directiva try_files 
## para resolver las rutas del lado del cliente de Blazor y asegura el tipo MIME correcto para los archivos .wasm.

#nginx
    
server {
    listen 80;
    server_name localhost;

    location / {
        root   /usr/share/nginx/html;
        index  index.html;
        try_files $uri $uri/ /index.html;
    }

    # Asegurar el tipo MIME correcto para archivos WebAssembly
    types {
        application/wasm wasm;
    }
}