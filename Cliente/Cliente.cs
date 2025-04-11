using Common.Config;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cliente;

public class Cliente
{
    private Socket _socket;
    private StreamReader _reader;
    private StreamWriter _writer;

    public Cliente()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public void Conectar()
    {
        _socket.Connect(IPAddress.Parse(ClientConfig.IPServidor), ClientConfig.PuertoServidor);
        NetworkStream stream = new NetworkStream(_socket);
        _reader = new StreamReader(stream, Encoding.UTF8);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        Console.WriteLine("Conectado al servidor.");
    }

    public void Enviar(string mensaje) => _writer.WriteLine(mensaje);
    public string Recibir() => _reader.ReadLine();
}