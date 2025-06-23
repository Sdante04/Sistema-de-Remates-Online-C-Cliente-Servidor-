using Grpc.Net.Client;
using Remate.GRPC;

namespace ClienteAdministrativo;

public class ClienteAdministrativo
{
    private GrpcChannel _channel;
    public Administracion.AdministracionClient ClienteGrpc { get; private set; }

    public async Task ConectarAsync()
    {
        _channel = GrpcChannel.ForAddress("https://localhost:7286");
        ClienteGrpc = new Administracion.AdministracionClient(_channel);
        await Task.CompletedTask;
    }

    public void Cerrar()
    {
        _channel?.Dispose();
    }

    public async Task EnviarArchivoPorPartesAsync(string path)
    {
        FileInfo info = new(path);
        string filename = info.Name;
        long fileLength = info.Length;
        const int MaxFileSizePart = 1024 * 512;

        using FileStream stream = File.OpenRead(path);
        long offset = 0;
        int parte = 1;
        long totalPartes = (long)Math.Ceiling((double)fileLength / MaxFileSizePart);

        while (offset < fileLength)
        {
            int bytesToRead = (int)Math.Min(MaxFileSizePart, fileLength - offset);
            byte[] buffer = new byte[bytesToRead];
            int read = await stream.ReadAsync(buffer, 0, bytesToRead);

            var request = new SubirArchivoRequest
            {
                NombreArchivo = filename,
                Datos = Google.Protobuf.ByteString.CopyFrom(buffer),
                EsUltimaParte = (offset + read) >= fileLength
            };

            var response = await ClienteGrpc.SubirArchivoAsync(request);
            if (!response.Ok)
            {
                Console.WriteLine($"Error al subir parte {parte}: {response.Mensaje}");
                return;
            }

            Console.WriteLine($"Parte {parte++}/{totalPartes} enviada ({read} bytes)");
            offset += read;
        }

        Console.WriteLine("Archivo enviado correctamente por gRPC.");
    }
}
