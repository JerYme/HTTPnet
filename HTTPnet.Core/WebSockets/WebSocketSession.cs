using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HTTPnet.Core.Communication;
using HTTPnet.Core.WebSockets.Protocol;

namespace HTTPnet.Core.WebSockets
{
    public class WebSocketSession : ISessionHandler
    {
        private readonly List<WebSocketFrame> _frameQueue = new List<WebSocketFrame>();
        private readonly WebSocketFrameWriter _webSocketFrameWriter;
        private readonly WebSocketFrameReader _webSocketFrameReader;
        private readonly ClientSession _clientSession;

        public WebSocketSession(ClientSession clientSession)
        {
            _clientSession = clientSession ?? throw new ArgumentNullException(nameof(clientSession));

            _webSocketFrameWriter = new WebSocketFrameWriter(_clientSession.Client.SendStream);
            _webSocketFrameReader = new WebSocketFrameReader(_clientSession.Client.ReceiveStream);
        }

        public event EventHandler<WebSocketMessageReceivedEventArgs> MessageReceived;

        public event EventHandler Closed;

        public async Task ProcessAsync()
        {
            var webSocketFrame = await _webSocketFrameReader.ReadAsync(_clientSession.CancellationToken).ConfigureAwait(false);
            switch (webSocketFrame.Opcode)
            {
                case WebSocketOpcode.Ping:
                    {
                        webSocketFrame.Opcode = WebSocketOpcode.Pong;
                        await _webSocketFrameWriter.WriteAsync(webSocketFrame, _clientSession.CancellationToken).ConfigureAwait(false);
                        return;
                    }

                case WebSocketOpcode.ConnectionClose:
                    {
                        await CloseAsync().ConfigureAwait(false);
                        return;
                    }

            }

            if (webSocketFrame.Opcode.IsControl()) return;

            ValidateNewFrame(webSocketFrame);

            _frameQueue.Add(webSocketFrame);
            if (webSocketFrame.Fin)
            {
                var message = GenerateMessage();
                _frameQueue.Clear();

                MessageReceived?.Invoke(this, new WebSocketMessageReceivedEventArgs(message, this));
            }
        }

        private void ValidateNewFrame(WebSocketFrame webSocketFrame)
        {
            // Details: https://tools.ietf.org/html/rfc6455#section-5.6 PAGE 34
            var first = _frameQueue.Count == 0;
            if (first)
            {
                if (webSocketFrame.Opcode != WebSocketOpcode.Binary &&
                    webSocketFrame.Opcode != WebSocketOpcode.Text)
                {
                    throw new InvalidOperationException("Frame opcode is invalid.");
                }
            }
            else if (!webSocketFrame.Fin)
            {
                if (webSocketFrame.Opcode != WebSocketOpcode.Continuation)
                {
                    throw new InvalidOperationException("Fragmented frame is invalid.");
                }
            }
        }

        public Task CloseAsync()
        {
            _clientSession.Close();
            Closed?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(0);
        }

        public async Task SendAsync(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            await _webSocketFrameWriter.WriteAsync(new WebSocketFrame
            {
                Opcode = WebSocketOpcode.Text,
                Payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text))
            }, _clientSession.CancellationToken).ConfigureAwait(false);
        }

        public async Task SendAsync(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            await _webSocketFrameWriter.WriteAsync(new WebSocketFrame
            {
                Opcode = WebSocketOpcode.Binary,
                Payload = new ArraySegment<byte>(data)
            }, _clientSession.CancellationToken).ConfigureAwait(false);
        }

        private WebSocketMessage GenerateMessage()
        {
            var messageType = _frameQueue[0].Opcode;

            if (messageType == WebSocketOpcode.Text)
            {
                using (var streamReader = new StreamReaderPeekable(new ArraySegmentsStream(_frameQueue.Select(x => x.Payload)), Encoding.UTF8, false, 1024, true))
                {
                    var text = streamReader.ReadToEnd();
                    return new WebSocketTextMessage(text);
                }
            }

            if (messageType == WebSocketOpcode.Binary)
            {
                var bytes = new byte[_frameQueue.Sum(x => x.Payload.Count)];
                int offset = 0;
                foreach (var frame in _frameQueue)
                {
                    var payload = frame.Payload;
                    if (payload.Array != null) Buffer.BlockCopy(payload.Array, payload.Offset, bytes, offset, payload.Count);
                    offset += payload.Count;
                }

                return new WebSocketBinaryMessage(bytes);
            }

            throw new NotSupportedException();
        }
        
    }
}
