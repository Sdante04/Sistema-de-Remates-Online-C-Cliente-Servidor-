using Common;

namespace GrpcService.Services
{
    public class ReporteRequestService
    {
        public static async Task<Guid> GenerarReporteAsync(string usuario, string? webhook = null)
        {
            var payload = new
            {
                usuario,
                webhook
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var rpcClient = new RpcClient();
            await rpcClient.StartAsync();

            var response = await rpcClient.CallAsync(json);
            await rpcClient.DisposeAsync();

            return Guid.Parse(response);
        }
    }
}
