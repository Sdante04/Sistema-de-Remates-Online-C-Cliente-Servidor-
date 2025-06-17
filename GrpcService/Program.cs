using GrpcService;
using GrpcService.Services;
using Servidor;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Levanta el servidor socket como una tarea paralela
        _ = Task.Run(() => Server.IniciarAsync());


        // Levanta el servidor gRPC
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
}

