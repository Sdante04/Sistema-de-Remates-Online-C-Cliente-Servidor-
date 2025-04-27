using System.Net;
using System.Net.Sockets;
using Servidor.Utils;
using Common.Config;
using Servidor.Servicios;

namespace Servidor
{
    public class Server
    {
        private static int contadorClientes = 0;
        private static readonly List<HiloCliente> _clientesActivos = new();
        private static readonly object _lockClientes = new object();
        private static readonly ConfigManager ConfigManager = new ConfigManager();

        private static bool _ejecutando = true;
        private static Socket? _socketServidor;
        private static readonly ArticuloServicio _articuloServicioCompartido = new();

        public static void Main()
        {
            Logger.Log("Levantando servidor...");

            _socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string serverIp = ConfigManager.Readsettings(ServerConfiguration.serverIPconfigKey);
            int serverPort = int.Parse(ConfigManager.Readsettings(ServerConfiguration.serverPortConfKey));
            var localEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _socketServidor.Bind(localEndpoint);
            _socketServidor.Listen();

            Logger.Log($"Esperando por clientes en la IP: {localEndpoint.Address} y escuchando en puerto: {localEndpoint.Port}");
            Logger.Log("Escriba 'exit' o 'cerrar' para cerrar el servidor");

            Thread hiloComandos = new Thread(EscucharComandoCierre);
            hiloComandos.Start();

            while (_ejecutando)
            {
                try
                {
                    Socket socketCliente = _socketServidor.Accept();

                    int idCliente = Interlocked.Increment(ref contadorClientes);
                    Logger.Log($"Nuevo cliente conectado (ID: {idCliente})");

                    var hiloCliente = new HiloCliente(socketCliente, idCliente, _articuloServicioCompartido);

                    lock (_lockClientes)
                    {
                        _clientesActivos.Add(hiloCliente);
                    }

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

            lock (_lockClientes)
            {
                foreach (var cliente in _clientesActivos)
                {
                    cliente.Cerrar();
                }
            }

            Logger.Log("Servidor finalizado correctamente");
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
    }
}
