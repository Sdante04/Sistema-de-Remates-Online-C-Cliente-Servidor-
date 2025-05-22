using Common.Config;
using Common;
using System.Net.Sockets;
using System.Text;
namespace Cliente;

public class Cliente
{
    private static readonly ConfigManager ConfigManager = new ConfigManager();
    private Socket _socket;
    private NetworkHelper _helper;

    public Cliente()
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task ConectarAsync()
    {
        string serverIp = ConfigManager.Readsettings(ClientConfiguration.serverIPconfigKey);
        int serverPort = int.Parse(ConfigManager.Readsettings(ClientConfiguration.serverPortConfKey));
        await _socket.ConnectAsync(serverIp, serverPort);
        _helper = new NetworkHelper(_socket);
        Console.WriteLine("Conectado al servidor.");
    }

    public async Task EnviarComandoAsync(int comando, object datos)
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

        await _helper.SendAsync(header);
        await _helper.SendAsync(cmd);
        await _helper.SendAsync(length);
        await _helper.SendAsync(dataBytes);
    }

    public async Task<(string respuesta, int comando)> RecibirRespuestaAsync()
    {
        try
        {
            byte[] header = await _helper.ReceiveAsync(ProtocolConstants.HEADER_SIZE);
            if (Encoding.UTF8.GetString(header) != ProtocolConstants.Response)
            {
                return ("Error: Respuesta inválida del servidor.", -1);
            }

            int comando = int.Parse(Encoding.UTF8.GetString(await _helper.ReceiveAsync(ProtocolConstants.CMD_SIZE)));
            int len = BitConverter.ToInt32(await _helper.ReceiveAsync(ProtocolConstants.LENGTH_SIZE));
            string datos = Encoding.UTF8.GetString(await _helper.ReceiveAsync(len));
            return (datos, comando);
        }
        catch (Exception ex)
        {
            return ($"Error al recibir respuesta: {ex.Message}", -1);
        }
    }

    public async Task EnviarArchivoPorPartesAsync(string path)
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
        await EnviarComandoAsync(CommandConstants.EnviarImagenHeader, headerData);

        while (offset < fileLength)
        {
            int bytesToSend = (int)Math.Min(ProtocoloImagen.MaxFileSizePart, fileLength - offset);
            byte[] buffer = fsHelper.Read(path, offset, bytesToSend);
            await EnviarComandoAsync(CommandConstants.EnviarImagenParte, buffer);
            Console.WriteLine($"Enviando parte {currentPart}/{totalParts} de {filename} ({bytesToSend} bytes)...");
            offset += bytesToSend;
            currentPart++;
        }

        Console.WriteLine("Archivo enviado correctamente por partes.");
    }

    public async Task RecibirArchivoPorPartesAsync()
    {
        FileStreamHelper fsHelper = new();

        byte[] headerPrefix = await _helper.ReceiveAsync(ProtocolConstants.HEADER_SIZE);
        string tipoHeader = Encoding.UTF8.GetString(headerPrefix);

        if (tipoHeader != ProtocolConstants.Response)
        {
            Console.WriteLine("Error: Protocolo de respuesta inválido para recibir imagen.");
            return;
        }

        int cmd = int.Parse(Encoding.UTF8.GetString(await _helper.ReceiveAsync(ProtocolConstants.CMD_SIZE)));
        int len = BitConverter.ToInt32(await _helper.ReceiveAsync(ProtocolConstants.LENGTH_SIZE));
        byte[] headerData = await _helper.ReceiveAsync(len);

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
            byte[] partHeader = await _helper.ReceiveAsync(ProtocolConstants.HEADER_SIZE);
            string tipoParte = Encoding.UTF8.GetString(partHeader);
            if (tipoParte != ProtocolConstants.Response)
            {
                Console.WriteLine("Error: Protocolo inválido en parte de imagen.");
                return;
            }

            int cmdParte = int.Parse(Encoding.UTF8.GetString(await _helper.ReceiveAsync(ProtocolConstants.CMD_SIZE)));
            int lenParte = BitConverter.ToInt32(await _helper.ReceiveAsync(ProtocolConstants.LENGTH_SIZE));
            byte[] dataParte = await _helper.ReceiveAsync(lenParte);

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
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        _socket.Close();
    }
}