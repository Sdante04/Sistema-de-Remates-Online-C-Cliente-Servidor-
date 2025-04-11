using System.Net.Sockets;
using System.Text;
using Servidor.Servicios;

namespace Servidor.Utils;

public class HiloCliente
{
    private Socket _socket;
    private int _idCliente;
    private UsuarioServicio _usuarioServicio;

    public HiloCliente(Socket socket, int id)
    {
        _socket = socket;
        _idCliente = id;
        _usuarioServicio = new UsuarioServicio();
    }

    public void Atender()
    {
        try
        {
            NetworkStream stream = new NetworkStream(_socket);
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (true)
            {
                string? mensaje = reader.ReadLine();
                if (mensaje == null) break;

                string[] partes = mensaje.Split('|');
                string comando = partes[0];

                switch (comando.ToUpper())
                {
                    case "LOGIN":
                        string usuario = partes[1];
                        string clave = partes[2];
                        var u = _usuarioServicio.Autenticar(usuario, clave);
                        writer.WriteLine(u != null ? "LOGIN_OK" : "LOGIN_FAIL");
                        break;

                    default:
                        writer.WriteLine("COMANDO_DESCONOCIDO");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error con cliente: " + ex.Message);
        }
        finally
        {
            _socket.Close();
            Logger.Log($"Cliente {_idCliente} desconectado.");
        }
    }
}
