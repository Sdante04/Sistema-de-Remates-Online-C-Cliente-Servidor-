# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copiamos todo el contenido de la solución (necesario para los ProjectReferences como Common)
COPY . .

# Restauramos y publicamos
RUN dotnet restore ServidorEstadisticasRest/ServidorEstadisticasRest.csproj
RUN dotnet publish ServidorEstadisticasRest/ServidorEstadisticasRest.csproj -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copiamos los archivos publicados desde el build stage
COPY --from=build /app/publish .

# Exponemos el puerto de la API REST
EXPOSE 80

# Comando que se ejecuta al iniciar el contenedor
ENTRYPOINT ["dotnet", "ServidorEstadisticasRest.dll"]
