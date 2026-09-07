# WebDocker

cd C:\DesApp\WebDocker

# [WebDocker]
## ------------------------------------------------------------------------------------------------
dotnet restore
dotnet build
dotnet run --project WebDocker\WebDocker\WebDocker.csproj --launch-profile https
dotnet watch --project WebDocker\WebDocker\WebDocker.csproj --launch-profile https

# Clean Publish
# 1. Borrar la carpeta de publicación anterior:
Remove-Item -Recurse -Force "C:\DesApp\WebDocker\publish\WebDocker" -ErrorAction SilentlyContinue

# 2. Limpiar los artefactos de compilación (build limpio):
dotnet clean "C:\DesApp\WebDocker\WebDocker\WebDocker\WebDocker.csproj" -c Release

# 3. Publicar de nuevo:
dotnet publish "C:\DesApp\WebDocker\WebDocker\WebDocker\WebDocker.csproj" -c Release -o "C:\DesApp\WebDocker\publish\WebDocker"


# [WDockerClient]
## ------------------------------------------------------------------------------------------------
dotnet restore WDockerClient\WDockerClient.csproj
dotnet build WDockerClient\WDockerClient.csproj
dotnet run --project WDockerClient\WDockerClient.csproj --launch-profile https
dotnet watch --project WDockerClient\WDockerClient.csproj --launch-profile https


# Clean Publish
# 1. Borrar la carpeta de publicación anterior:
Remove-Item -Recurse -Force "C:\DesApp\WebDocker\publish\WDockerClient" -ErrorAction SilentlyContinue

# 2. Limpiar los artefactos de compilación (build limpio):
dotnet clean "C:\DesApp\WebDocker\WDockerClient\WDockerClient.csproj" -c Release

# 3. Publicar de nuevo:
dotnet publish "C:\DesApp\WebDocker\WDockerClient\WDockerClient.csproj" -c Release -o "C:\DesApp\WebDocker\publish\WDockerClient"



# [API]
## ------------------------------------------------------------------------------------------------
Login con curl/Postman:
POST https://localhost:7210/api/auth/login
Content-Type: application/json
{ "userName": "Admin", "password": "Admin" }

# WbDocker/Api/ApiAuth.http

# [Login]
## ------------------------------------------------------------------------------------------------
curl.exe -k -X POST "https://localhost:7210/api/auth/login" -H "Content-Type: application/json" -d '{\"userName\":\"Admin\",\"password\":\"Admin\"}'
curl.exe -i -k -X POST "https://localhost:7210/api/auth/login" -H "Content-Type: application/json" -d '{\"userName\":\"Admin\",\"password\":\"Admin\"}'

# [Servers]
## ------------------------------------------------------------------------------------------------
$token = (curl.exe -k -s -X POST "https://localhost:7210/api/auth/login" -H "Content-Type: application/json" -d '{\"userName\":\"Admin\",\"password\":\"Admin\"}' | ConvertFrom-Json).token
curl.exe -k "https://localhost:7210/api/servers" -H "Authorization: Bearer $token"
curl.exe -k -s "https://localhost:7210/api/servers" -H "Authorization: Bearer $token" | ConvertFrom-Json | ConvertTo-Json




# [Imagen]
## ------------------------------------------------------------------------------------------------
wdockerclient:latest -> cliente WDockerClient (Blazor WebAssembly)

# Se construye con Docker de WSL sobre OCI por arquitectura ARM64
# contenedor de prueba -> wdc-test

# build Image 51.170.39.109
wsl docker --host tcp://51.170.39.109:2376 --tlsverify --tlscacert /mnt/c/DesApp/WebDocker/.certs/ca.pem --tlscert /mnt/c/DesApp/WebDocker/.certs/cert.pem --tlskey /mnt/c/DesApp/WebDocker/.certs/key.pem build -f /mnt/c/DesApp/WebDocker/WDockerClient/Docker/Dockerfile -t wdockerclient:latest /mnt/c/DesApp/WebDocker/WDockerClient

# comprobar la imagen generada
wsl docker --host tcp://51.170.39.109:2376 --tlsverify --tlscacert /mnt/c/DesApp/WebDocker/.certs/ca.pem --tlscert /mnt/c/DesApp/WebDocker/.certs/cert.pem --tlskey /mnt/c/DesApp/WebDocker/.certs/key.pem image inspect wdockerclient:latest --format "{{.Os}}/{{.Architecture}}"

# descargar la imagen .tar
wsl docker --host tcp://51.170.39.109:2376 --tlsverify --tlscacert /mnt/c/DesApp/WebDocker/.certs/ca.pem --tlscert /mnt/c/DesApp/WebDocker/.certs/cert.pem --tlskey /mnt/c/DesApp/WebDocker/.certs/key.pem save wdockerclient:latest -o /mnt/c/DesApp/WebDocker/publish/wdockerclient.tar


## ------------------------------------------------------------------------------------------------
# [Certificados]
/mnt/c/DesApp/WebDocker/WebDocker/WebDocker/DataBase/seed-certs
/mnt/c/DesApp/WebDocker/.certs

# [Docker]
/mnt/c/DesApp/WebDocker/WDockerClient/Docker