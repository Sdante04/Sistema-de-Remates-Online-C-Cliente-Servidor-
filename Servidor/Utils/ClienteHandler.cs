using Servidor.Dominio;
using Servidor.Servicios;
using System.Net.Sockets;
using System.Text;
using Common;
using Servidor.Utils;

namespace Servidor.Utils
{
    public class ClienteHandler
    {
        private readonly Socket _socket;
        private readonly int _id;
        private readonly UsuarioServicio _us;
        private readonly ArticuloServicio _articuloServicio;
        private readonly NetworkHelper _helper;
        private string? _usuarioActual;
        private string _archivoActualNombre = "";
        private long _archivoActualTamanio = 0;
        private long _archivoActualOffset = 0;
        private readonly string _rutaImagenes = Environment.GetEnvironmentVariable("SERVER_IMAGE_PATH") ?? "/app/imagenes";

        public ClienteHandler(Socket socket, int id, ArticuloServicio articuloServicio)
        {
            _socket = socket;
            _id = id;
            _us = new UsuarioServicio();
            _articuloServicio = articuloServicio;
            _helper = new NetworkHelper(_socket);
        }

        public async Task AtenderAsync()
        {
            try
            {
                while (true)
                {
                    string header = Encoding.UTF8.GetString(await _helper.ReceiveAsync(ProtocolConstants.HEADER_SIZE));
                    int cmd = int.Parse(Encoding.UTF8.GetString(await _helper.ReceiveAsync(ProtocolConstants.CMD_SIZE)));
                    int len = BitConverter.ToInt32(await _helper.ReceiveAsync(ProtocolConstants.LENGTH_SIZE));
                    byte[] rawData = await _helper.ReceiveAsync(len);
                    string data = Encoding.UTF8.GetString(rawData);

                    string resp = await ProcesarComandoAsync(cmd, data, rawData);

                    if (!string.IsNullOrEmpty(resp))
                    {
                        byte[] rb = Encoding.UTF8.GetBytes(resp);
                        await _helper.SendAsync(Encoding.UTF8.GetBytes("RES"));
                        await _helper.SendAsync(Encoding.UTF8.GetBytes(cmd.ToString("D2")));
                        await _helper.SendAsync(BitConverter.GetBytes(rb.Length));
                        await _helper.SendAsync(rb);
                    }
                }
            }
            catch (SocketException ex)
            {
                Logger.Log($"[Cliente {_id}] Conexión cerrada: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Cliente {_id}] Error inesperado: {ex.Message}");
            }
            finally
            {
                _socket.Close();
                Logger.Log($"Cliente {_id} desconectado.");
            }
        }

        private async Task<string> ProcesarComandoAsync(int cmd, string data, byte[] rawData)
        {
            switch (cmd)
            {
                case CommandConstants.Login:
                    var parts = data.Split('|');
                    var usuario = parts[0];
                    var usuarioAutenticado = _us.Autenticar(parts[0], parts[1]);
                    if (usuarioAutenticado != null)
                    {
                        _usuarioActual = usuario;
                        Logger.Log($"[Cliente {_id}] Usuario '{usuario}' inició sesión.");
                        return "LOGIN_OK";
                    }
                    return "LOGIN_FAIL";

                case CommandConstants.RegistrarUsuario:
                    var partesRegistro = data.Split('|');
                    bool registrado = _us.RegistrarUsuario(partesRegistro[0], partesRegistro[1]);
                    if (registrado)
                    {
                        Logger.Log($"[Cliente {_id}] Usuario '{partesRegistro[0]}' registrado.");
                        return "REGISTRO_OK";
                    }
                    return "USUARIO_YA_EXISTE";

                case CommandConstants.ValidarArticulo:
                    return _articuloServicio.ValidarDatosArticulo(data) ? "VALIDO" : "INVALIDO";

                case CommandConstants.PublicarArticulo:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    bool ok;
                    string msg = _articuloServicio.PublicarArticulo(data, _usuarioActual, out ok);
                    return msg;

                case CommandConstants.EditarArticulo:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    bool okEditar;
                    return _articuloServicio.EditarArticulo(data, _usuarioActual, out okEditar);

                case CommandConstants.EliminarArticulo:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    bool eliminado;
                    return _articuloServicio.EliminarArticulo(data, _usuarioActual, out eliminado);

                case CommandConstants.RealizarOferta:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    return _articuloServicio.RealizarOferta(data, _usuarioActual);

                case CommandConstants.ObtenerArticulosUsuario:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    return _articuloServicio.ObtenerArticulosDeUsuario(_usuarioActual);

                case CommandConstants.ListarArticulosRemate:
                    return _articuloServicio.ObtenerTodosLosArticulosEnRemate();

                case CommandConstants.ListarTodosLosArticulos:
                    return _articuloServicio.ObtenerTodosLosArticulos();

                case CommandConstants.FiltrarArticulosPorCategoria:
                    return _articuloServicio.FiltrarArticulosPorCategoria(data);

                case CommandConstants.ConsultarArticulo:
                    return _articuloServicio.ConsultarArticulo(data);

                case CommandConstants.ObtenerOfertasUsuario:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    return _articuloServicio.ObtenerOfertasDeUsuario(_usuarioActual);

                case CommandConstants.ObtenerRematesGanados:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    return _articuloServicio.ObtenerRematesGanadosPorUsuario(_usuarioActual);

                case CommandConstants.EnviarImagenHeader:
                    int nameLen = BitConverter.ToInt32(rawData, 0);
                    string filename = Encoding.UTF8.GetString(rawData, 4, nameLen);
                    long fileSize = BitConverter.ToInt64(rawData, 4 + nameLen);

                    string rutaImagenes = Environment.GetEnvironmentVariable("SERVER_IMAGE_PATH") ?? "/app/imagenes";
                    string fullPath = Path.Combine(rutaImagenes, filename);

                    if (File.Exists(fullPath))
                        File.Delete(fullPath);

                    _archivoActualNombre = fullPath;
                    _archivoActualTamanio = fileSize;
                    _archivoActualOffset = 0;

                    Logger.Log($"[Cliente {_id}] Preparado para recibir archivo '{fullPath}' ({fileSize} bytes)");
                    return "";

                case CommandConstants.EnviarImagenParte:
                    if (string.IsNullOrEmpty(_archivoActualNombre)) return "Error: faltó encabezado.";
                    new FileStreamHelper().Write(_archivoActualNombre, rawData);
                    _archivoActualOffset += rawData.Length;
                    if (_archivoActualOffset >= _archivoActualTamanio)
                    {
                        _archivoActualNombre = "";
                        _archivoActualOffset = 0;
                        _archivoActualTamanio = 0;
                    }
                    return "";

                case CommandConstants.ListarArticulosConImagen:
                    return _articuloServicio.ListarArticulosConImagen();

                case CommandConstants.SolicitarImagenArticulo:
                    if (_usuarioActual == null) return "NO_AUTENTICADO";
                    if (!int.TryParse(data, out int idImg)) return "ID inválido";
                    string? path = _articuloServicio.ObtenerNombreArchivoImagen(idImg);
                    if (path != null)
                    {
                        await EnviarArchivoAlClienteAsync(path);
                        return "";
                    }
                    return "IMAGEN_NO_ENCONTRADA";

                default:
                    return "CMD_DESCONOCIDO";
            }
        }

        private async Task EnviarArchivoAlClienteAsync(string path)
        {
            FileStreamHelper fsHelper = new();
            string fullPath = Path.Combine(_rutaImagenes, Path.GetFileName(path));
            FileInfo info = new(fullPath);
            string filename = info.Name;
            long fileLength = info.Length;

            byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
            byte[] filenameLengthBytes = BitConverter.GetBytes(filenameBytes.Length);
            byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
            byte[] headerData = filenameLengthBytes.Concat(filenameBytes).Concat(fileLengthBytes).ToArray();

            await EnviarComandoAsync(CommandConstants.EnviarImagenHeader, headerData);

            long offset = 0;
            while (offset < fileLength)
            {
                int bytesToSend = (int)Math.Min(ProtocoloImagen.MaxFileSizePart, fileLength - offset);
                byte[] buffer = fsHelper.Read(path, offset, bytesToSend);
                await EnviarComandoAsync(CommandConstants.EnviarImagenParte, buffer);
                offset += bytesToSend;
            }
        }

        private async Task EnviarComandoAsync(int cmd, byte[] data)
        {
            await _helper.SendAsync(Encoding.UTF8.GetBytes(ProtocolConstants.Response));
            await _helper.SendAsync(Encoding.UTF8.GetBytes(cmd.ToString("D2")));
            await _helper.SendAsync(BitConverter.GetBytes(data.Length));
            await _helper.SendAsync(data);
        }

        public void Cerrar()
        {
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            _socket.Close();
        }
    }
}