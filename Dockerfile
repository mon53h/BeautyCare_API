# 1. Imagen base para compilar (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar archivos del proyecto
COPY ["BeautyCare_API.csproj", "./"]
RUN dotnet restore

# Copiar el resto de archivos
COPY . .

# Publicar la aplicación en modo Release
RUN dotnet publish -c Release -o /app/publish

# 2. Imagen base para ejecutar (ASP.NET runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar build publicado
COPY --from=build /app/publish .

# Render usa PORT, así que exponemos el puerto correctamente
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Ejecutar la API
ENTRYPOINT ["dotnet", "BeautyCare_API.dll"]
