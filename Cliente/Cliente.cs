using Common.Config;
using Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cliente
{
    public class Cliente
    {
        private Socket _socket;
        private NetworkHelper _helper;

        public Cliente()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Conectar()
        {
            _socket.Connect(ClientConfig.IPServidor, ClientConfig.PuertoServidor);
            _helper = new NetworkHelper(_socket);
            Console.WriteLine("Conectado al servidor.");
        }

        public void IniciarMonitoreoCierre()
        {
            var hilo = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        if (_socket.Poll(0, SelectMode.SelectRead) && _socket.Available == 0)
                        {
                            Console.WriteLine("\n Conexión del servidor finalizada");
                            Environment.Exit(0);
                        }
                    }
                    catch
                    {
                        Environment.Exit(0);
                    }
                    Thread.Sleep(500);
                }
            });

            hilo.IsBackground = true;
            hilo.Start();
        }

        public void EnviarComando(int comando, string datos)
        {
            byte[] header = Encoding.UTF8.GetBytes(ProtocolConstants.Request);
            byte[] cmd = Encoding.UTF8.GetBytes(comando.ToString("D2"));
            byte[] dataBytes = Encoding.UTF8.GetBytes(datos);
            byte[] length = BitConverter.GetBytes(dataBytes.Length);

            _helper.Send(header);
            _helper.Send(cmd);
            _helper.Send(length);
            _helper.Send(dataBytes);
        }

        public string RecibirRespuesta(out int comando)
        {
            byte[] header = _helper.Receive(ProtocolConstants.HEADER_SIZE);
            comando = int.Parse(Encoding.UTF8.GetString(_helper.Receive(ProtocolConstants.CMD_SIZE)));
            int len = BitConverter.ToInt32(_helper.Receive(ProtocolConstants.LENGTH_SIZE));
            string datos = Encoding.UTF8.GetString(_helper.Receive(len));
            return datos;
        }
    }
}



