using GrpcService;


namespace ServidorGrpc
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Levanta el servidor TCP/IP como tarea paralela
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
}

