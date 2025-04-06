using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cliente;

public class Cliente
{
    private Socket _socket;

    public Cliente()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Conectar(string ip, int puerto)
    {
        _socket.Connect(new IPEndPoint(IPAddress.Parse(ip), puerto));
        Console.WriteLine("Conectado al servidor.");
    }

    public void Enviar(string mensaje)
    {
        byte[] mensajeBytes = Encoding.UTF8.GetBytes(mensaje);
        byte[] largo = BitConverter.GetBytes(mensajeBytes.Length);
        _socket.Send(largo);
        _socket.Send(mensajeBytes);
    }

    public string Recibir()
    {
        byte[] largoBytes = new byte[4];
        _socket.Receive(largoBytes);
        int largo = BitConverter.ToInt32(largoBytes, 0);

        byte[] buffer = new byte[largo];
        _socket.Receive(buffer);

        return Encoding.UTF8.GetString(buffer);
    }
}