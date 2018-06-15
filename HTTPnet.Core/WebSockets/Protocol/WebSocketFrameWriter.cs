using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPnet.Core.WebSockets.Protocol
{
    public class WebSocketFrameWriter
    {
        private readonly Stream _sendStream;
        private readonly byte[] _header;

        public WebSocketFrameWriter(Stream sendStream)
        {
            _sendStream = sendStream ?? throw new ArgumentNullException(nameof(sendStream));
            _header = new byte[14];
        }

        public async Task WriteAsync(WebSocketFrame frame, CancellationToken cancellationToken)
        {
            // https://tools.ietf.org/html/rfc6455

            var headerSize = 2;

            if (frame.Fin)
            {
                _header[0] |= 128;
            }

            _header[0] |= (byte)frame.Opcode;

            var maskingKey = frame.MaskingKey;
            if (maskingKey != null)
            {
                _header[1] |= 128;
            }

            var payloadLength = frame.Payload.Count;

            if (payloadLength > 0)
            {
                if (payloadLength <= 125)
                {
                    _header[1] |= (byte)payloadLength;
                }
                else if (payloadLength >= 126 && payloadLength <= 65535)
                {
                    _header[1] |= 126;
                    _header[2] = (byte)(payloadLength >> 8);
                    _header[3] = (byte)payloadLength;
                    headerSize += 2;
                }
                else
                {
                    _header[1] |= 127;
                    _header[2] = (byte)(payloadLength >> 56);
                    _header[3] = (byte)(payloadLength >> 48);
                    _header[4] = (byte)(payloadLength >> 40);
                    _header[5] = (byte)(payloadLength >> 32);
                    _header[6] = (byte)(payloadLength >> 24);
                    _header[7] = (byte)(payloadLength >> 16);
                    _header[8] = (byte)(payloadLength >> 8);
                    _header[9] = (byte)payloadLength;
                    headerSize += 8;
                }
            }

            if (maskingKey != null)
            {
                _header[headerSize] |= maskingKey[0];
                _header[headerSize + 1] |= maskingKey[1];
                _header[headerSize + 2] |= maskingKey[2];
                _header[headerSize + 3] |= maskingKey[3];
                headerSize += 4;
            }

            frame.Mask();

            await _sendStream.WriteAsync(_header, 0, headerSize, cancellationToken).ConfigureAwait(false);
            await _sendStream.WriteAsync(frame.Payload.Array, frame.Payload.Offset, payloadLength, cancellationToken).ConfigureAwait(false);
            await _sendStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
