using Servidor.Dominio;
using Servidor.Servicios;
using System.Net.Sockets;
using System.Text;
using Common;
using Servidor.Utils;

namespace Servidor.Utils
{
    public class HiloCliente
    {
        private Socket _socket;
        private int _id;
        private UsuarioServicio _us;
        private bool _activo = true;
        private ArticuloServicio _articuloServicio;
        private string _archivoActualNombre = "";
        private long _archivoActualTamanio = 0;
        private long _archivoActualOffset = 0;
        private string _usuarioActual = null;

        public HiloCliente(Socket socket, int id, ArticuloServicio articuloServicio)
        {
            _socket = socket;
            _id = id;
            _us = new UsuarioServicio();
            _articuloServicio = articuloServicio;
        }

        public void Atender()
        {
            var helper = new NetworkHelper(_socket);
            try
            {
                while (_activo)
                {
                    string header = Encoding.UTF8.GetString(helper.Receive(ProtocolConstants.HEADER_SIZE));
                    int cmd = int.Parse(Encoding.UTF8.GetString(helper.Receive(ProtocolConstants.CMD_SIZE)));
                    int len = BitConverter.ToInt32(helper.Receive(ProtocolConstants.LENGTH_SIZE));
                    byte[] rawData = helper.Receive(len);
                    string data = Encoding.UTF8.GetString(rawData);

                    string resp = "";
                    switch (cmd)
                    {
                        case CommandConstants.Login:
                            var parts = data.Split('|');
                            var usuario = parts[0];
                            var usuarioAutenticado = _us.Autenticar(parts[0], parts[1]);
                            if (usuarioAutenticado != null)
                            {
                                _usuarioActual = usuario;
                                Logger.Log($"[Cliente {_id}] Usuario '{usuario}' inició sesión correctamente.");
                                resp = "LOGIN_OK";
                            }
                            else
                            {
                                Logger.Warn($"[Cliente {_id}] Fallo de login para usuario '{usuario}'.");
                                resp = "LOGIN_FAIL";
                            }
                            break;

                        case CommandConstants.RegistrarUsuario:
                            var partesRegistro = data.Split('|');
                            bool registrado = _us.RegistrarUsuario(partesRegistro[0], partesRegistro[1]);
                            if (registrado)
                            {
                                Logger.Log($"[Cliente {_id}] Usuario '{partesRegistro[0]}' registrado correctamente.");
                                resp = "REGISTRO_OK";
                            }
                            else
                            {
                                Logger.Warn($"[Cliente {_id}] Fallo al registrar usuario '{partesRegistro[0]}'. Ya existe o datos inválidos.");
                                resp = "USUARIO_YA_EXISTE";
                            }
                            break;

                        case CommandConstants.PublicarArticulo:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            bool ok;
                            string mensaje = _articuloServicio.PublicarArticulo(data, _usuarioActual, out ok);
                            if (ok)
                            {
                                var partes = data.Split('|');
                                string titulo = partes[0];
                                Logger.Log($"[Cliente {_id}] Usuario '{_usuarioActual}' publicó el artículo '{titulo}'.");
                            }
                            else
                            {
                                Logger.Warn($"[Cliente {_id}] Fallo al publicar artículo. Datos: {data}");
                            }
                            resp = mensaje;
                            break;

                        case CommandConstants.EnviarImagenHeader:
                            int nameLen = BitConverter.ToInt32(rawData, 0);
                            string filename = Encoding.UTF8.GetString(rawData, 4, nameLen);
                            long fileSize = BitConverter.ToInt64(rawData, 4 + nameLen);

                            if (File.Exists(filename))
                                File.Delete(filename);

                            _archivoActualNombre = filename;
                            _archivoActualTamanio = fileSize;
                            _archivoActualOffset = 0;
                            Logger.Log($"[Cliente {_id}] Recibiendo archivo '{filename}' ({fileSize} bytes)");
                            break;

                        case CommandConstants.EnviarImagenParte:
                            if (string.IsNullOrEmpty(_archivoActualNombre))
                            {
                                Logger.Error("No se recibió el encabezado del archivo antes de recibir partes.");
                                break;
                            }

                            new FileStreamHelper().Write(_archivoActualNombre, rawData);
                            _archivoActualOffset += rawData.Length;

                            if (_archivoActualOffset >= _archivoActualTamanio)
                            {
                                Logger.Log($"[Cliente {_id}] Imagen '{_archivoActualNombre}' recibida completamente.");
                                _archivoActualNombre = "";
                                _archivoActualTamanio = 0;
                                _archivoActualOffset = 0;
                            }
                            break;

                        case CommandConstants.ValidarArticulo:
                            bool esValido = _articuloServicio.ValidarDatosArticulo(data);
                            resp = esValido ? "VALIDO" : "INVALIDO";
                            break;

                        case CommandConstants.ObtenerArticulosUsuario:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            resp = _articuloServicio.ObtenerArticulosDeUsuario(_usuarioActual);
                            break;

                        case CommandConstants.EditarArticulo:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            bool okEditar;
                            string mensajeEditar = _articuloServicio.EditarArticulo(data, _usuarioActual, out okEditar);
                            resp = mensajeEditar;
                            if (okEditar)
                                Logger.Log($"[Cliente {_id}] Usuario '{_usuarioActual}' editó un artículo.");
                            else
                                Logger.Warn($"[Cliente {_id}] Fallo al editar artículo: {mensajeEditar}");
                            break;

                        case CommandConstants.RealizarOferta:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            string respuesta = _articuloServicio.RealizarOferta(data, _usuarioActual);
                            resp = respuesta;
                            if (resp.Contains("Oferta registrada:"))
                                Logger.Log($"[Cliente {_id}] {respuesta}");
                            else
                                Logger.Warn($"[Cliente {_id}] {respuesta}");
                            break;

                        case CommandConstants.ListarArticulosRemate:
                            resp = _articuloServicio.ObtenerTodosLosArticulosEnRemate();
                            break;

                        case CommandConstants.ConsultarArticulo:
                            string respuestaConsultar = _articuloServicio.ConsultarArticulo(data);
                            resp = respuestaConsultar;
                            break;

                        case CommandConstants.ListarTodosLosArticulos:
                            resp = _articuloServicio.ObtenerTodosLosArticulos();
                            break;

                        case CommandConstants.ListarArticulosConImagen:
                            var lista = _articuloServicio.ListarArticulosConImagen();
                            resp = lista;
                            break;

                        case CommandConstants.SolicitarImagenArticulo:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            if (int.TryParse(data, out int id))
                            {
                                var path = _articuloServicio.ObtenerNombreArchivoImagen(id);
                                if (path != null)
                                {
                                    EnviarArchivoAlCliente(helper, path);
                                }
                                else
                                {
                                    resp = "IMAGEN_NO_ENCONTRADA";
                                }
                            }
                            else
                            {
                                resp = "ID inválido.";
                            }
                            break;

                        case CommandConstants.EliminarArticulo:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            bool okEliminar;
                            string mensajeEliminar = _articuloServicio.EliminarArticulo(data, _usuarioActual, out okEliminar);
                            resp = mensajeEliminar;
                            if (okEliminar)
                                Logger.Log($"[Cliente {_id}] Usuario '{_usuarioActual}' eliminó un artículo.");
                            else
                                Logger.Warn($"[Cliente {_id}] Fallo al eliminar artículo: {mensajeEliminar}");
                            break;

                        case CommandConstants.FiltrarArticulosPorCategoria:
                            resp = _articuloServicio.FiltrarArticulosPorCategoria(data);
                            break;

                        case CommandConstants.ObtenerOfertasUsuario:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            resp = _articuloServicio.ObtenerOfertasDeUsuario(_usuarioActual);
                            break;

                        case CommandConstants.ObtenerRematesGanados:
                            if (_usuarioActual == null)
                            {
                                resp = "NO_AUTENTICADO";
                                break;
                            }
                            resp = _articuloServicio.ObtenerRematesGanadosPorUsuario(_usuarioActual);
                            break;

                        default:
                            resp = "CMD_DESCONOCIDO";
                            break;
                    }

                    if (!string.IsNullOrEmpty(resp))
                    {
                        byte[] rb = Encoding.UTF8.GetBytes(resp);
                        helper.Send(Encoding.UTF8.GetBytes("RES"));
                        helper.Send(Encoding.UTF8.GetBytes(cmd.ToString("D2")));
                        helper.Send(BitConverter.GetBytes(rb.Length));
                        helper.Send(rb);
                    }
                }
            }
            catch (SocketException sockEx)
            {
                Logger.Log($"[Cliente {_id}] Conexión cerrada: {sockEx.Message}");
                _activo = false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Cliente {_id}] Error inesperado: {ex.Message}");
                _activo = false;
            }
            finally
            {
                _socket.Close();
                Logger.Log($"Cliente {_id} desconectado.");
            }
        }

        private void EnviarArchivoAlCliente(NetworkHelper helper, string path)
        {
            FileStreamHelper fsHelper = new();
            FileInfo info = new(path);
            string filename = info.Name;
            long fileLength = info.Length;
            long totalParts = ProtocoloImagen.CalcularCantidadDePartes(fileLength);

            byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
            byte[] filenameLengthBytes = BitConverter.GetBytes(filenameBytes.Length);
            byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
            byte[] headerData = filenameLengthBytes.Concat(filenameBytes).Concat(fileLengthBytes).ToArray();

            EnviarComandoDesdeServidor(helper, CommandConstants.EnviarImagenHeader, headerData);

            long offset = 0;
            long currentPart = 1;
            while (offset < fileLength)
            {
                int bytesToSend = (int)Math.Min(ProtocoloImagen.MaxFileSizePart, fileLength - offset);
                byte[] buffer = fsHelper.Read(path, offset, bytesToSend);

                EnviarComandoDesdeServidor(helper, CommandConstants.EnviarImagenParte, buffer);

                offset += bytesToSend;
                currentPart++;
            }

            Logger.Log($"Imagen '{filename}' enviada correctamente al cliente.");
        }


         private void EnviarComandoDesdeServidor(NetworkHelper helper, int cmd, byte[] data)
         {
             helper.Send(Encoding.UTF8.GetBytes(ProtocolConstants.Response));
             helper.Send(Encoding.UTF8.GetBytes(cmd.ToString("D2")));
             helper.Send(BitConverter.GetBytes(data.Length));
             helper.Send(data);
         }


        public void Cerrar()
        {
            _activo = false;
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            _socket.Close();
        }
    }
}