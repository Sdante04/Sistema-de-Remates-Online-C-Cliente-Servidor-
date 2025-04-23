using Common.Config;
using Common;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Common.Common;

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

        public void EnviarComando(int comando, object datos)
        {
            byte[] dataBytes;

            if (datos is string texto)
                dataBytes = Encoding.UTF8.GetBytes(texto);
            else if (datos is byte[] bytes)
                dataBytes = bytes;
            else
                throw new ArgumentException("Tipo de datos no soportado. Usa string o byte[].");

            byte[] header = Encoding.UTF8.GetBytes(ProtocolConstants.Request);
            byte[] cmd = Encoding.UTF8.GetBytes(comando.ToString("D2"));
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

        public void EnviarArchivoPorPartes(string path)
        {
            FileInfo info = new(path);
            string filename = info.Name;
            long fileLength = info.Length;
            long totalParts = Protocolo.CalcularCantidadDePartes(fileLength);

            FileStreamHelper fsHelper = new();
            long offset = 0;
            long currentPart = 1;

            // CMD 3: Enviar encabezado (nombre + tamaño)
            byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
            byte[] filenameLengthBytes = BitConverter.GetBytes(filenameBytes.Length);
            byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
            byte[] headerData = filenameLengthBytes.Concat(filenameBytes).Concat(fileLengthBytes).ToArray();
            EnviarComando(CommandConstants.EnviarImagenHeader, headerData);

            // CMD 4: Enviar partes individuales
            while (offset < fileLength)
            {
                int bytesToSend = (int)Math.Min(Protocolo.MaxFileSizePart, fileLength - offset);
                byte[] buffer = fsHelper.Read(path, offset, bytesToSend);
                EnviarComando(CommandConstants.EnviarImagenParte, buffer);
                Console.WriteLine($"Enviando parte {currentPart}/{totalParts} de {filename}...");
                offset += bytesToSend;
                currentPart++;
            }

            Console.WriteLine("Archivo enviado correctamente por partes.");
        }


    }


}



