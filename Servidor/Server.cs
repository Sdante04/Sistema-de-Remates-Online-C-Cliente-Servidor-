using System.Net.Sockets;
using System.Threading.Tasks;

namespace Common
{
    public class NetworkHelper
    {
        private readonly Socket _socket;
        public NetworkHelper(Socket socket) => _socket = socket;

        public async Task SendAsync(byte[] data)
        {
            int offset = 0;
            while (offset < data.Length)
            {
                int sent = await _socket.SendAsync(data.AsMemory(offset), SocketFlags.None);
                if (sent == 0) throw new SocketException();
                offset += sent;
            }
        }

        public async Task<byte[]> ReceiveAsync(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int received = await _socket.ReceiveAsync(buffer.AsMemory(offset), SocketFlags.None);
                if (received == 0) throw new SocketException();
                offset += received;
            }
            return buffer;
        }
    }
}