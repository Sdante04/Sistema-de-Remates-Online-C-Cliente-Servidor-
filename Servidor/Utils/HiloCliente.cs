using Servidor.Servicios;
using Servidor.Utils;
using System.Net.Sockets;
using System.Text;
using Servidor.Servicios;
using Common;
using Common.Common;

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

        public HiloCliente(Socket socket, int id, ArticuloServicio articuloServicio)
        {
            _socket = socket; _id = id; _us = new UsuarioServicio();
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
                            if (_us.Autenticar(parts[0], parts[1]) != null)
                            {
                                Logger.Log($"[Cliente {_id}] Usuario '{usuario}' inició sesión correctamente.");
                                resp = "LOGIN_OK";
                            }
                            else
                            {
                                Logger.Warn($"[Cliente {_id}] Fallo de login para usuario '{usuario}'.");
                                resp = "LOGIN_FAIL";
                            }
                            break;

                        case CommandConstants.PublicarArticulo:
                            {
                                string user = $"cliente_{_id}";
                                bool ok;
                                string mensaje = _articuloServicio.PublicarArticulo(data, user, out ok);
                                if (ok)
                                {
                                    var partes = data.Split('|');
                                    string titulo = partes[0];
                                    Logger.Log($"[Cliente {_id}] Usuario '{user}' publicó el artículo '{titulo}'.");
                                }
                                else
                                {
                                    Logger.Warn($"[Cliente {_id}] Fallo al publicar artículo. Datos: {data}");
                                }
                                resp = mensaje;
                                break;
                            }

                        case CommandConstants.EnviarImagenHeader:
                            {
                                int nameLen = BitConverter.ToInt32(rawData, 0);
                                string filename = Encoding.UTF8.GetString(rawData, 4, nameLen);
                                long fileSize = BitConverter.ToInt64(rawData, 4 + nameLen);

                                _archivoActualNombre = filename;
                                _archivoActualTamanio = fileSize;
                                _archivoActualOffset = 0;

                                Logger.Log($"[Cliente {_id}] Recibiendo archivo '{filename}' ({fileSize} bytes)");
                                break;
                            }

                        case CommandConstants.EnviarImagenParte:
                            {
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
                            }
                        case CommandConstants.ValidarArticulo:
                            {
                                bool esValido = _articuloServicio.ValidarDatosArticulo(data);
                                resp = esValido ? "VALIDO" : "INVALIDO";
                                break;
                            }

                        case CommandConstants.ObtenerArticulosUsuario:
                            {
                                string user = $"cliente_{_id}";
                                resp = _articuloServicio.ObtenerArticulosDeUsuario(user);
                                break;
                            }

                        case CommandConstants.EditarArticulo:
                            {
                                string user = $"cliente_{_id}";
                                bool ok;
                                string mensaje = _articuloServicio.EditarArticulo(data, user, out ok);
                                resp = mensaje;
                                if (ok)
                                    Logger.Log($"[Cliente {_id}] Usuario '{user}' editó un artículo.");
                                else
                                    Logger.Warn($"[Cliente {_id}] Fallo al editar artículo: {mensaje}");
                                break;
                            }
                        case CommandConstants.RealizarOferta:
                            {
                                string user = $"cliente_{_id}";
                                string respuesta = _articuloServicio.RealizarOferta(data, user);
                                resp = respuesta;
                                if (resp.Contains("Oferta registrada:"))
                                    Logger.Log($"[Cliente {_id}] {respuesta}");
                                else
                                    Logger.Warn($"[Cliente {_id}] {respuesta}");
                                break;
                            }
                        case CommandConstants.ListarArticulosRemate:
                            {
                                resp = _articuloServicio.ObtenerTodosLosArticulosEnRemate();
                                break;
                            }
                        case CommandConstants.ConsultarArticulo:
                            {
                                string respuesta = _articuloServicio.ConsultarArticulo(data);
                                resp = respuesta;
                                break;
                            }
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
            catch { /* cierre intencional o error */ }
            finally
            {
                _socket.Close();
                Logger.Log($"Cliente {_id} desconectado.");
            }
        }

        public void Cerrar()
        {
            _activo = false;
            try { _socket.Shutdown(SocketShutdown.Both); } catch { }
            _socket.Close();
        }
    }

}