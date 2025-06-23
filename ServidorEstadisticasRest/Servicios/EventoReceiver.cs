using Common;
using Common.Models;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace ServidorEstadisticasRest.Servicios;

public class EventoReceiver : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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

                EventoBase? evento = tipo switch
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
                Console.WriteLine($"[!] Error procesando evento: {ex.Message}");
            }

            await Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

    
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
