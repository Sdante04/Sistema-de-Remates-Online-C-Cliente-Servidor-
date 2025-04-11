using System.Net;
using System.Net.Sockets;
using Servidor.Utils;
using Common.Config;

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
}
