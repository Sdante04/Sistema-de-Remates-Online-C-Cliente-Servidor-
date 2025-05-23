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
        private static readonly List<ClienteHandler> _clientesActivos = new();
        private static readonly object _lockClientes = new();
        private static readonly ConfigManager ConfigManager = new();

        private static bool _ejecutando = true;
        private static Socket? _socketServidor;
        private static readonly ArticuloServicio _articuloServicioCompartido = new();

        public static async Task Main()
        {
            Logger.Log("Levantando servidor...");

            _socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string serverIp = ConfigManager.Readsettings(ServerConfiguration.serverIPconfigKey);
            int serverPort = int.Parse(ConfigManager.Readsettings(ServerConfiguration.serverPortConfKey));
            var localEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _socketServidor.Bind(localEndpoint);
            _socketServidor.Listen();

            Logger.Log($"Esperando por clientes en {localEndpoint.Address}:{localEndpoint.Port}");

            AppDomain.CurrentDomain.ProcessExit += (s, e) => _ejecutando = false;

            while (_ejecutando)
            {
                try
                {
                    Socket socketCliente = await _socketServidor.AcceptAsync();

                    int idCliente = Interlocked.Increment(ref contadorClientes);
                    Logger.Log($"Nuevo cliente conectado (ID: {idCliente})");

                    var handler = new ClienteHandler(socketCliente, idCliente, _articuloServicioCompartido);

                    lock (_lockClientes)
                    {
                        _clientesActivos.Add(handler);
                    }

                    _ = Task.Run(() => handler.AtenderAsync()); 
                }
                catch (SocketException ex)
                {
                    if (!_ejecutando)
                        Logger.Log("Socket cerrado intencionalmente.");
                    else
                        Logger.Error("Error inesperado al aceptar cliente: " + ex.Message);
                }
            }

            Logger.Log("Cerrando conexiones activas...");

            lock (_lockClientes)
            {
                foreach (var cliente in _clientesActivos)
                    cliente.Cerrar();
            }

            Logger.Log("Servidor finalizado.");
        }
    }
}
