# Multi-stage build para .NET Framework
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019 AS build

# Instalar NuGet
RUN powershell -Command "Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile 'C:\nuget.exe'"

WORKDIR /src

# Copiar archivos de proyecto
COPY ["GestorTarea.csproj", "./"]
COPY ["packages.config", "./"]

# Restaurar paquetes NuGet
RUN C:\nuget.exe restore

# Copiar el resto del código fuent
COPY . .

# Compilar el proyecto
RUN msbuild GestorTarea.csproj /p:Configuration=Release /p:Platform="Any CPU"

# Imagen final para runtime
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2019
WORKDIR /inetpub/wwwroot

# Copiar la aplicación compilada
COPY --from=build /src .

EXPOSE 80