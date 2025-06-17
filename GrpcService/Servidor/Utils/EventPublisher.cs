using Common.Models;
using RabbitMQ.Client;
using System.Text;
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
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "admin"
            };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync("eventos", ExchangeType.Fanout);

            var json = JsonSerializer.Serialize(evento, evento.GetType(), new JsonSerializerOptions
            {
                PropertyNamingPolicy = null
            });

            var body = Encoding.UTF8.GetBytes(json);

            await channel.BasicPublishAsync(exchange: "eventos", routingKey: "", body: body);

        }
    }
}

