using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Servidor.Utils;
using Servidor.Servicios;

namespace Servidor
{
    public class Server
    {
        private readonly ArticuloServicio _articuloServicio;
        private readonly List<ClienteHandler> _clientesActivos = new();
        private readonly object _lockClientes = new();
        private volatile bool _ejecutando = true;
        private Socket? _socketServidor;
        private int _contadorClientes = 0;

        public Server(ArticuloServicio articuloServicio)
        {
            _articuloServicio = articuloServicio;
        }

        public async Task IniciarAsync()
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

            // Tarea para detectar cierre por consola
            _ = Task.Run(() =>
            {
                while (_ejecutando)
                {
                    string? input = Console.ReadLine();
                    if (input?.Trim().ToLower() == "salir")
                    {
                        Logger.Log("Cierre solicitado por consola.");
                        _ejecutando = false;
                        try { _socketServidor?.Close(); } catch { }
                    }
                }
            });

            while (_ejecutando)
            {
                try
                {
                    var socketCliente = await _socketServidor.AcceptAsync();

                    int idCliente = Interlocked.Increment(ref _contadorClientes);
                    Logger.Log($"Nuevo cliente conectado (ID: {idCliente})");

                    var handler = new ClienteHandler(socketCliente, idCliente, _articuloServicio);
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
