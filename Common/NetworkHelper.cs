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

        public byte[] Receive(int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int rec = _socket.Receive(buffer, offset, length - offset, SocketFlags.None);
                if (rec == 0) throw new SocketException();
                offset += rec;
            }
            return buffer;
        }
    }
}
