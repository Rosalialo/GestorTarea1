## Usar imagen más ligera
#FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8
#
## Establecer directorio de trabajo
#WORKDIR /inetpub/wwwroot
#
## Copiar todos los archivos de la aplicación
#COPY . .
#
## Exponer puerto 80
#EXPOSE 80


# Dockerfile para producción - ASP.NET Framework
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8

# Establecer directorio de trabajo
WORKDIR /inetpub/wwwroot

# Copiar archivos de la aplicación
COPY . .

# Configurar IIS para producción
RUN powershell -NoProfile -Command \
    Import-module IISAdministration; \
    Remove-IISSite -Name 'Default Web Site' -Confirm:$false; \
    New-IISSite -Name 'GestorTarea' -PhysicalPath 'C:\inetpub\wwwroot' -Port 80

# Exponer puerto 80 (HTTP) y 443 (HTTPS)
EXPOSE 80
EXPOSE 443

# Configurar variables de entorno
ENV ASPNETCORE_ENVIRONMENT=Production