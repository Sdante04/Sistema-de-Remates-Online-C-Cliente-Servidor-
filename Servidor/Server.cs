using System.Net;
using System.Net.Sockets;
using Servidor.Utils;
using Servidor.Servicios;

namespace Servidor
{
    public class Server
    {
        private static int contadorClientes = 0;
        private static readonly List<ClienteHandler> _clientesActivos = new();
        private static readonly object _lockClientes = new();
        private static bool _ejecutando = true;
        private static Socket? _socketServidor;
        private static readonly ArticuloServicio _articuloServicioCompartido = new();


        public static async Task Main()
        {
            Logger.Log("Levantando servidor...");

            _socketServidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string serverIp = Environment.GetEnvironmentVariable("SERVER_IP") ?? "127.0.0.1";
            int serverPort = int.Parse(Environment.GetEnvironmentVariable("SERVER_PORT") ?? "5000");
            var localEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _socketServidor.Bind(localEndpoint);
            _socketServidor.Listen();

            Logger.Log($"Esperando por clientes en {localEndpoint.Address}:{localEndpoint.Port}");
            Logger.Log("Escriba 'salir' y presione Enter para cerrar el servidor.");

            _ = Task.Run(() =>
            {
                while (true)
                {
                    string? input = Console.ReadLine();
                    if (input?.Trim().ToLower() == "salir")
                    {
                        Logger.Log("Cierre solicitado por consola.");
                        _ejecutando = false;
                        try { _socketServidor?.Close(); } catch { }
                        break;
                    }
                }
            });

            while (_ejecutando)
            {
                try
                {
                    var articuloServicio = _articuloServicioCompartido;
                    var usuarioServicio = new UsuarioServicio();
                    var cargador = new CargadorInicial(_articuloServicioCompartido, usuarioServicio);
                    Logger.Log("Cargando datos iniciales desde archivos...");
                    await cargador.CargarTodoAsync();
                    Logger.Log("Carga inicial completada.");
                    
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
