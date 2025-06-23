using Common.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ServidorEstadisticasRest.Servicios
{
    public static class ReportesManager
    {
        private static ConcurrentDictionary<Guid, EstadoReporte> reportes = new();

        public static Guid GenerarReporte(string usuario, string? webhook = null)
        {
            var id = Guid.NewGuid();
            var estado = new EstadoReporte
            {
                Id = id,
                Usuario = usuario,
                Estado = "pendiente",
                Webhook = webhook
            };

            reportes[id] = estado;

            _ = Task.Run(async () =>
            {

                var resultado = await EstadisticasService.ObtenerEventosPorUsuario(usuario);
                estado.Resultado = resultado;
                estado.Estado = "completo";

                if (!string.IsNullOrEmpty(webhook))
                {
                    try
                    {
                        var json = JsonSerializer.Serialize(new { id, estado = "completo" });
                        using var http = new HttpClient();
                        await http.PostAsync(webhook, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[!] Error al enviar webhook: {ex.Message}");
                    }
                }
            });

            return id;
        }

        public static string ObtenerEstado(Guid id) =>
            reportes.TryGetValue(id, out var r) ? r.Estado : "desconocido";

        public static Dictionary<string, int>? ObtenerResultado(Guid id) =>
            reportes.TryGetValue(id, out var r) && r.Estado == "completo" ? r.Resultado : null;
    }
}
