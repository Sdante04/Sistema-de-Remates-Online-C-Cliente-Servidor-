using Common.Config;
using Common;
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
                            Console.WriteLine("\nConexión del servidor finalizada");
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
            try
            {
                byte[] header = _helper.Receive(ProtocolConstants.HEADER_SIZE);
                if (Encoding.UTF8.GetString(header) != ProtocolConstants.Response)
                {
                    comando = -1;
                    return "Error: Respuesta inválida del servidor.";
                }

                comando = int.Parse(Encoding.UTF8.GetString(_helper.Receive(ProtocolConstants.CMD_SIZE)));
                int len = BitConverter.ToInt32(_helper.Receive(ProtocolConstants.LENGTH_SIZE));
                string datos = Encoding.UTF8.GetString(_helper.Receive(len));
                return datos;
            }
            catch (Exception ex)
            {
                comando = -1;
                return $"Error al recibir respuesta: {ex.Message}";
            }
        }

        public void EnviarArchivoPorPartes(string path)
        {
            FileInfo info = new(path);
            string filename = info.Name;
            long fileLength = info.Length;
            long totalParts = ProtocoloImagen.CalcularCantidadDePartes(fileLength);

            FileStreamHelper fsHelper = new();
            long offset = 0;
            long currentPart = 1;

            byte[] filenameBytes = Encoding.UTF8.GetBytes(filename);
            byte[] filenameLengthBytes = BitConverter.GetBytes(filenameBytes.Length);
            byte[] fileLengthBytes = BitConverter.GetBytes(fileLength);
            byte[] headerData = filenameLengthBytes.Concat(filenameBytes).Concat(fileLengthBytes).ToArray();
            EnviarComando(CommandConstants.EnviarImagenHeader, headerData);

            while (offset < fileLength)
            {
                int bytesToSend = (int)Math.Min(ProtocoloImagen.MaxFileSizePart, fileLength - offset);
                byte[] buffer = fsHelper.Read(path, offset, bytesToSend);
                EnviarComando(CommandConstants.EnviarImagenParte, buffer);
                Console.WriteLine($"Enviando parte {currentPart}/{totalParts} de {filename} ({bytesToSend} bytes)...");
                offset += bytesToSend;
                currentPart++;
            }

            Console.WriteLine("Archivo enviado correctamente por partes.");
        }

        public void RecibirArchivoPorPartes()
        {
            var helper = _helper;
            FileStreamHelper fsHelper = new();

            byte[] headerPrefix = helper.Receive(ProtocolConstants.HEADER_SIZE);
            string tipoHeader = Encoding.UTF8.GetString(headerPrefix);

            if (tipoHeader != ProtocolConstants.Response)
            {
                Console.WriteLine("Error: Protocolo de respuesta inválido para recibir imagen.");
                return;
            }

            int cmd = int.Parse(Encoding.UTF8.GetString(helper.Receive(ProtocolConstants.CMD_SIZE)));
            int len = BitConverter.ToInt32(helper.Receive(ProtocolConstants.LENGTH_SIZE));
            byte[] headerData = helper.Receive(len);

            if (cmd != CommandConstants.EnviarImagenHeader)
            {
                Console.WriteLine("Error: No se recibió encabezado correcto de imagen.");
                return;
            }

            int nameLen = BitConverter.ToInt32(headerData, 0);
            string filename = Encoding.UTF8.GetString(headerData, 4, nameLen);
            long fileSize = BitConverter.ToInt64(headerData, 4 + nameLen);

            Console.WriteLine($"Recibiendo archivo: {filename} ({fileSize} bytes)");

            long offset = 0;
            long currentPart = 1;

            while (offset < fileSize)
            {
                byte[] partHeader = helper.Receive(ProtocolConstants.HEADER_SIZE);
                string tipoParte = Encoding.UTF8.GetString(partHeader);
                if (tipoParte != ProtocolConstants.Response)
                {
                    Console.WriteLine("Error: Protocolo inválido en parte de imagen.");
                    return;
                }

                int cmdParte = int.Parse(Encoding.UTF8.GetString(helper.Receive(ProtocolConstants.CMD_SIZE)));
                int lenParte = BitConverter.ToInt32(helper.Receive(ProtocolConstants.LENGTH_SIZE));
                byte[] dataParte = helper.Receive(lenParte);

                if (cmdParte != CommandConstants.EnviarImagenParte)
                {
                    Console.WriteLine("Error: Esperaba parte de imagen, recibió otro comando.");
                    return;
                }

                fsHelper.Write(filename, dataParte);
                Console.WriteLine($"Parte {currentPart++} recibida ({lenParte} bytes)...");
                offset += lenParte;
            }

            Console.WriteLine($"Archivo '{filename}' recibido correctamente.");
        }


        public void Cerrar()
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            finally
            {
                _socket.Close();
            }
        }
    }
}