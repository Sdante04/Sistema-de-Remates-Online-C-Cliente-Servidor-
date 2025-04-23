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
        private ArticuloServicio _articuloServicio = new();

        public HiloCliente(Socket socket, int id)
        {
            _socket = socket; _id = id; _us = new UsuarioServicio();
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
                    string data = Encoding.UTF8.GetString(helper.Receive(len));

                    string resp = "";
                    switch (cmd)
                    {
                        case CommandConstants.Login:
                            var parts = data.Split('|');
                            resp = _us.Autenticar(parts[0], parts[1]) != null ? "LOGIN_OK" : "LOGIN_FAIL";
                            break;
                        case CommandConstants.PublicarArticulo:
                            {
                                // Usuario simulado para pruebas, en un futuro integrar con servicio de autenticación
                                string usuario = $"cliente_{_id}";
                                bool ok;
                                string mensaje = _articuloServicio.PublicarArticulo(data, usuario, out ok);
                                resp = mensaje;
                                break;
                            }
                        default:
                            resp = "CMD_DESCONOCIDO";
                            break;
                    }
                    byte[] rb = Encoding.UTF8.GetBytes(resp);
                    helper.Send(Encoding.UTF8.GetBytes("RES"));
                    helper.Send(Encoding.UTF8.GetBytes(cmd.ToString("D2")));
                    helper.Send(BitConverter.GetBytes(rb.Length));
                    helper.Send(rb);
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
