FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

COPY . .

RUN dotnet restore
RUN dotnet publish Servidor/Servidor.csproj -c Release -o ./publish

EXPOSE 5000
ENTRYPOINT ["dotnet", "./publish/Servidor.dll"]
