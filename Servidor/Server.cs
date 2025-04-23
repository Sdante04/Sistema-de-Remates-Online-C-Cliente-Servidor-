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
    private static List<HiloCliente> _clientesActivos = new();
    private static bool _ejecutando = true;
    private static Socket? _socketServidor;

    public static void Main()
    {
        Logger.Log("Levantando servidor...");

        _socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socketServidor.Bind(new IPEndPoint(IPAddress.Parse(ServerConfig.IP), ServerConfig.Puerto));
        _socketServidor.Listen();

        Logger.Log($"Servidor escuchando en puerto {ServerConfig.Puerto}");
        Logger.Log($"Escriba 'exit' o 'cerrar' para cerrar el servidor");

        Thread hiloComandos = new Thread(EscucharComandoCierre);
        hiloComandos.Start();

        while (_ejecutando)
        {
            try
            {
                Socket socketCliente = _socketServidor.Accept(); 

                int idCliente = Interlocked.Increment(ref contadorClientes);
                Logger.Log($"Nuevo cliente conectado (ID: {idCliente})");

                var hiloCliente = new HiloCliente(socketCliente, idCliente);
                _clientesActivos.Add(hiloCliente);

                Thread hilo = new Thread(hiloCliente.Atender);
                hilo.Start();
            }
            catch (SocketException ex)
            {
                if (!_ejecutando)
                    Logger.Log("Socket cerrado intencionalmente. Deteniendo servidor...");
                else
                    Logger.Error("Error inesperado al aceptar cliente: " + ex.Message);
            }
        }

        Logger.Log("Cerrando conexiones activas...");
        foreach (var cliente in _clientesActivos)
        {
            cliente.Cerrar();
        }

        Logger.Log("Servidor finalizado correctamente.");
    }

    private static void EscucharComandoCierre()
    {
        while (true)
        {
            string? comando = Console.ReadLine();
            if (comando?.ToLower() is "exit" or "cerrar")
            {
                Logger.Log("Se recibió comando de cierre. Apagando servidor...");
                _ejecutando = false;

                try
                {
                    _socketServidor?.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error al cerrar el socket del servidor: " + ex.Message);
                }

                break;
            }
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
