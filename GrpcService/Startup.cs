using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Servidor.Servicios;

namespace GrpcService;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddGrpc();
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<AdministracionServicio>();

            endpoints.MapControllers();

            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Servidor gRPC + HTTP está corriendo.");
            });
        });
    }
}
