using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using HTTPnet.Core.Communication;
using HTTPnet.Core.Http;

namespace HTTPnet.Implementations
{
    public class ClientSocketWrapper : IClientSocketWrapper
    {
        private readonly Socket _socket;
        private readonly HttpClientOptions _options;

        public ClientSocketWrapper(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));

            Identifier = socket.RemoteEndPoint.ToString();
            
            ReceiveStream = new NetworkStream(socket, true);
            SendStream = ReceiveStream;
        }

        public ClientSocketWrapper(HttpClientOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }


        public string Identifier { get; }

        public Stream ReceiveStream { get; }
        public Stream SendStream { get; }

        public Task DisconnectAsync()
        {
            _socket.Shutdown(SocketShutdown.Both);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            ReceiveStream?.Dispose();
            SendStream?.Dispose();

            _socket?.Dispose();
        }
    }
}
