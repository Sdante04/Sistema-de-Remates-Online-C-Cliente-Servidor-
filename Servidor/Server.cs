using System.Net;
using System.Net.Sockets;
using Servidor.Utils;

namespace Servidor;

class Server
{
    static void Main(string[] args)
    {
        Logger.Log("Levantando servidor...");

        Socket socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socketServidor.Bind(new IPEndPoint(IPAddress.Any, Config.PUERTO));
        socketServidor.Listen();

        Logger.Log($"Servidor escuchando en puerto {Config.PUERTO}");

        while (true) //mala practica, hay que corregir
        {
            Socket socketCliente = socketServidor.Accept();
            Logger.Log("Nuevo cliente conectado");

            Thread hilo = new Thread(() => new HiloCliente(socketCliente).Atender());
            hilo.Start();
        }
    }
}
