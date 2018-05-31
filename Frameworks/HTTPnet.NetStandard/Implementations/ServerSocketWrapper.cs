using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using HTTPnet.Core.Communication;
using HTTPnet.Core.Http;

namespace HTTPnet.Implementations
{
    public class ServerSocketWrapper : IServerSocketWrapper
    {
        private readonly HttpServerOptions _options;
        private Socket _listener;

        public ServerSocketWrapper(HttpServerOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task StartAsync()
        {
            if (_listener != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = _options.NoDelay
            };

            _listener.Bind(new IPEndPoint(IPAddress.Any, _options.Port));
            _listener.Listen(_options.Backlog);
            
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            if (_listener == null)
            {
                return Task.CompletedTask;
            }
            
            _listener.Shutdown(SocketShutdown.Both);
            _listener.Dispose();
            _listener = null;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _listener?.Shutdown(SocketShutdown.Both);
            _listener?.Dispose();
            _listener = null;
        }

        public async Task<IClientSocketWrapper> AcceptAsync()
        {
            var clientSocket = await _listener.AcceptAsync();
            clientSocket.NoDelay = _options.NoDelay;

            return new ClientSocketWrapper(clientSocket);
        }
    }
}
