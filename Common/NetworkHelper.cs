using System.Net.Sockets;

namespace Common
{
    public class NetworkHelper
    {
        private readonly Socket _socket;

        public NetworkHelper(Socket socket) => _socket = socket;

        public void Send(byte[] data)
        {
            int offset = 0;
            while (offset < data.Length)
            {
                int sent = _socket.Send(data, offset, data.Length - offset, SocketFlags.None);
                if (sent == 0) throw new SocketException();
                offset += sent;
            }
        }

        public byte[] Receive(int dataLength)
        {
            byte[] buffer = new byte[dataLength];
            int offset = 0;
            while (offset < dataLength)
            {
                int received = _socket.Receive(buffer, offset, dataLength - offset, SocketFlags.None);
                if (received == 0) throw new SocketException();
                offset += received;
            }
            return buffer;
        }
    }
}
