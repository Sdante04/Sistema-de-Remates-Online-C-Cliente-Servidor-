# Usa como base la imagen oficial de .NET 8 SDK (con herramientas para compilar apps)
FROM mcr.microsoft.com/dotnet/sdk:8.0


# Define el directorio de trabajo dentro del contenedor (/app)
WORKDIR /app


# Copia todos los archivos del proyecto local al directorio /app del contenedor
COPY . .

# Restaura las dependencias definidas en los archivos .csproj o .sln
RUN dotnet restore

# Publica el proyecto en modo Release y coloca los archivos compilados en /app/publish
RUN dotnet publish Servidor/Servidor.csproj -c Release -o ./publish


# Expone el puerto 5000 del contenedor (para que pueda ser mapeado al host)
EXPOSE 5000

# Define el comando que se ejecutará cuando el contenedor arranque
ENTRYPOINT ["dotnet", "./publish/Servidor.dll"]

