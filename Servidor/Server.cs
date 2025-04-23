using System.Net;
using System.Net.Sockets;
using Servidor.Utils;
using Common.Config;
using Common;
using System.Text;

namespace Servidor;

public class Server
{
    private static int contadorClientes = 0;

   public static void Main()
    {
        Logger.Log("Levantando servidor...");
        var socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketServidor.Bind(new IPEndPoint(IPAddress.Parse(ServerConfig.IP), ServerConfig.Puerto));
        socketServidor.Listen();

        Logger.Log($"Servidor escuchando en puerto {ServerConfig.Puerto}");

        while (true)
        {
            Socket socketCliente = socketServidor.Accept();
            int idCliente = Interlocked.Increment(ref contadorClientes);
            Logger.Log($"Nuevo cliente conectado (ID: {idCliente})");

            Thread hilo = new Thread(() => new HiloCliente(socketCliente, idCliente).Atender());
            hilo.Start();
        }
    }


    public void RecibirArchivoPorPartes(NetworkHelper helper)
    {
        FileStreamHelper fsHelper = new();

        int nameLen = BitConverter.ToInt32(helper.Receive(Protocolo.LargoFijo), 0);
        string filename = Encoding.UTF8.GetString(helper.Receive(nameLen));

        long fileSize = BitConverter.ToInt64(helper.Receive(Protocolo.LargoFijoArchivo), 0);
        long totalParts = Protocolo.CalcularCantidadDePartes(fileSize);

        long offset = 0;
        long currentPart = 1;

        while (offset < fileSize)
        {
            int bytesToReceive = (int)Math.Min(Protocolo.MaxFileSizePart, fileSize - offset);
            Console.WriteLine($"Recibiendo parte {currentPart}/{totalParts}...");
            byte[] buffer = helper.Receive(bytesToReceive);
            fsHelper.Write(filename, buffer);
            offset += bytesToReceive;
            currentPart++;
        }

        Logger.Log($"Archivo '{filename}' recibido correctamente.");
    }

}
