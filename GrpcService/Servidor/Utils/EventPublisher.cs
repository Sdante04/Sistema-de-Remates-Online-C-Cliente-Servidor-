using Common.Models;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Servidor.Servicios
{
    public static class EventPublisher
    {
        public static async Task PublicarEventoAsync(EventoBase evento)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("eventos", ExchangeType.Fanout);

            var json = JsonSerializer.Serialize(evento, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(exchange: "eventos", routingKey: "", body: body);
            Console.WriteLine($"[>>] Evento publicado: {evento.Tipo}");
        }
    }
}
