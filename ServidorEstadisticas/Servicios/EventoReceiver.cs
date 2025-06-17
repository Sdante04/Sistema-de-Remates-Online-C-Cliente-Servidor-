using Common;
using Common.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ServidorEstadisticas.Servicios
{
    public class EventoReceiver
    {
        public async Task StartAsync()
        {
            var (connection, channel, queueName) = await RabbitHelper.CrearConsumidorFanoutAsync("eventos");

            Console.WriteLine("[*] Escuchando eventos...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    string json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    using var doc = JsonDocument.Parse(json);
                    string tipo = doc.RootElement.GetProperty("Tipo").GetString();

                    EventoBase evento = tipo switch
                    {
                        "Login" => JsonSerializer.Deserialize<EventoUsuario>(json),
                        "Alta" or "Baja" or "Modificacion" => JsonSerializer.Deserialize<EventoArticulo>(json),
                        "Oferta" => JsonSerializer.Deserialize<EventoOferta>(json),
                        "RemateFinalizado" => JsonSerializer.Deserialize<EventoRemate>(json),
                        _ => null
                    };

                    if (evento != null)
                    {
                        AlmacenEventos.AgregarEvento(evento);
                        Console.WriteLine($"[x] Evento recibido: {tipo} ({evento.Fecha})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Error: {ex.Message}");
                }

                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            Console.WriteLine("Presioná ENTER para mostrar estadísticas de logins...");
            Console.ReadLine();

            var estadisticas = EstadisticasService.ObtenerEstadisticasDeLogins();

            Console.WriteLine("\n=== Estadísticas de logins por usuario ===");

            if (estadisticas.Count == 0)
            {
                Console.WriteLine("No se registraron logins.");
            }
            else
            {
                foreach (var entry in estadisticas)
                {
                    Console.WriteLine($"Usuario: {entry.Key} | Logins: {entry.Value}");
                }
            }

            Console.WriteLine("\nPresioná ENTER para salir...");
            Console.ReadLine();

        }
    }
}
