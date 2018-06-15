using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using HTTPnet.Core.Communication;

namespace HTTPnet.Core.Sockets
{
    public class ClientSocketWrapper : IClientSocketWrapper
    {
        private readonly Socket _socket;

        public ClientSocketWrapper(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            Identifier = socket.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

            var networkStream = new NetworkStream(socket, true);
            var bufferedStream = new BufferedStream(networkStream, 8192);
            ReceiveStream = bufferedStream;
            SendStream = bufferedStream;
        }

        public string Identifier { get; }

        public Stream ReceiveStream { get; }
        public Stream SendStream { get; }

        public Task DisconnectAsync()
        {
#if NETSTANDARD2_0
            _socket.Shutdown(SocketShutdown.Both);
#else
            //_socket.DisconnectAsync(new SocketAsyncEventArgs());
#endif
            return Task.FromResult(0);
        }

        public void Dispose()
        {
            ReceiveStream?.Dispose();
            SendStream?.Dispose();

            _socket?.Dispose();
        }
    }
}