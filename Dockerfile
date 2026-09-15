# Stage 1: Compilación y publicación con .NET 8 SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar dependencias de proyectos respetando la arquitectura en capas
COPY Backend/src/SubastaYa.Domain/SubastaYa.Domain.csproj Backend/src/SubastaYa.Domain/
COPY Backend/src/SubastaYa.Application/SubastaYa.Application.csproj Backend/src/SubastaYa.Application/
COPY Backend/src/SubastaYa.Infrastructure/SubastaYa.Infrastructure.csproj Backend/src/SubastaYa.Infrastructure/
COPY Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj Backend/src/SubastaYa.WebApi/

# Restaurar paquetes de la solución
RUN dotnet restore Backend/src/SubastaYa.WebApi/SubastaYa.WebApi.csproj

# Copiar el código fuente completo del backend
COPY Backend/src/SubastaYa.Domain/ Backend/src/SubastaYa.Domain/
COPY Backend/src/SubastaYa.Application/ Backend/src/SubastaYa.Application/
COPY Backend/src/SubastaYa.Infrastructure/ Backend/src/SubastaYa.Infrastructure/
COPY Backend/src/SubastaYa.WebApi/ Backend/src/SubastaYa.WebApi/

# Publicar la aplicación en modo Release
WORKDIR /src/Backend/src/SubastaYa.WebApi
RUN dotnet publish SubastaYa.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime ASP.NET Core 8
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar binarios compilados
COPY --from=build /app/publish .

# Copiar la interfaz web para soporte SPA embebido
COPY Fronted/SubastaYa-Fronted/wwwroot ./wwwroot

# Configuración de entorno y puertos
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SubastaYa.WebApi.dll"]
