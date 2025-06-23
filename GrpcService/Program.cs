using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using GrpcService;          
using Servidor;             
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task Main(string[] args)
    {
        // 1) Arranco el servidor central (TCP) en background
        var host = CreateHostBuilder(args).Build();
        var tcpServer = host.Services.GetRequiredService<Servidor.Server>();
        _ = Task.Run(() => tcpServer.IniciarAsync());
        await host.RunAsync();

        // 2) Construyo y arranco el host de ASP.NET Core con gRPC
        await CreateHostBuilder(args)
              .Build()
              .RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}
