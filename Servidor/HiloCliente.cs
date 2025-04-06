using System.Net.Sockets;
using System.Text;
using Servidor.Servicios;
using Servidor.Dominio;
using Servidor.Utils;

namespace Servidor;

public class HiloCliente
{
    private Socket _socket;
    private UsuarioServicio _usuarioServicio;

    public HiloCliente(Socket socket)
    {
        _socket = socket;
        _usuarioServicio = new UsuarioServicio();
    }

    public void Atender()
    {
        try
        {
            while (true)
            {
                string mensaje = RecibirMensaje();

                if (mensaje == null)
                    break;

                // Protocolo básico: LOGIN|usuario|clave
                string[] partes = mensaje.Split('|');
                string comando = partes[0];

                switch (comando.ToUpper())
                {
                    case "LOGIN":
                        string usuario = partes[1];
                        string clave = partes[2];

                        Usuario? u = _usuarioServicio.Autenticar(usuario, clave);
                        string respuesta = u != null ? "LOGIN_OK" : "LOGIN_FAIL";
                        EnviarMensaje(respuesta);
                        break;

                    default:
                        EnviarMensaje("COMANDO_DESCONOCIDO");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Error con cliente: " + ex.Message);
        }
        finally
        {
            _socket.Close();
            Logger.Log("Cliente desconectado.");
        }
    }

    private string RecibirMensaje()
    {
        byte[] largoBytes = new byte[4];
        int bytesRecibidos = _socket.Receive(largoBytes);
        if (bytesRecibidos == 0) return null;

        int largo = BitConverter.ToInt32(largoBytes, 0);
        byte[] buffer = new byte[largo];
        _socket.Receive(buffer);

        return Encoding.UTF8.GetString(buffer);
    }

    private void EnviarMensaje(string mensaje)
    {
        byte[] datos = Encoding.UTF8.GetBytes(mensaje);
        byte[] largo = BitConverter.GetBytes(datos.Length);
        _socket.Send(largo);
        _socket.Send(datos);
    }
}
