# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Copiamos todo el contenido de la solución (Common, GrpcService, etc.)
COPY . .

# Restauramos y publicamos desde el subdirectorio correcto
WORKDIR /app/GrpcService
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5000

ENTRYPOINT ["dotnet", "GrpcService.dll"]
