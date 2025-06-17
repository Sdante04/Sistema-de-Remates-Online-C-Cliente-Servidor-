using Grpc.Core;
using GrpcService;

namespace GrpcService.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    public GreeterService(ILogger<GreeterService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }

    public override async Task<FibonacciReply> CalcularFibonacci(FibonacciRequest request, ServerCallContext context)
    {
        await using var rpcClient = new RpcClient();
        await rpcClient.StartAsync();

        // Envía el valor al servidor RPC via RabbitMQ
        string response = await rpcClient.CallAsync(request.Valor.ToString());

        return new FibonacciReply { Resultado = response };
    }

}



