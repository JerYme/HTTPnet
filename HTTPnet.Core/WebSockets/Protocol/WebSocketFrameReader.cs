using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPnet.Core.WebSockets.Protocol
{
    public class WebSocketFrameReader
    {
        private readonly Stream _receiveStream;
        private readonly byte[] _headBuffer = new byte[1024];

        public WebSocketFrameReader(Stream receiveStream)
        {
            _receiveStream = receiveStream ?? throw new ArgumentNullException(nameof(receiveStream));
        }

        public async Task<WebSocketFrame> ReadAsync(CancellationToken cancellationToken)
        {
            /* https://tools.ietf.org/html/rfc6455
             0                   1                   2                   3
              0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
             +-+-+-+-+-------+-+-------------+-------------------------------+
             |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
             |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
             |N|V|V|V|       |S|             |   (if payload len==126/127)   |
             | |1|2|3|       |K|             |                               |
             +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
             |     Extended payload length continued, if payload len == 127  |
             + - - - - - - - - - - - - - - - +-------------------------------+
             |                               |Masking-key, if MASK set to 1  |
             +-------------------------------+-------------------------------+
             | Masking-key (continued)       |          Payload Data         |
             +-------------------------------- - - - - - - - - - - - - - - - +
             :                     Payload Data continued ...                :
             + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
             |                     Payload Data continued ...                |
             +---------------------------------------------------------------+
            */
            var webSocketFrame = new WebSocketFrame();

            var b01 = await ReadHeaderAsync(2, cancellationToken);
            var b0 = b01[0];
            var b1 = b01[1];

            if ((b0 & 128) == 128)
            {
                webSocketFrame.Fin = true;
                b0 = (byte)(127 & b0);
            }

            webSocketFrame.Opcode = (WebSocketOpcode)b0;

            var payloadLength = b1 & 127;
            if (payloadLength == 126)
            {
                // The length is 7 + 16 bits.
                var b23 = await ReadHeaderAsync(2, cancellationToken);
                var b2 = b23[0];
                var b3 = b23[1];

                payloadLength = b3 | b2 >> 8 | 126 >> 16;
            }
            else if (payloadLength == 127)
            {
                // The length is 7 + 64 bits.
                var b29 = await ReadHeaderAsync(8, cancellationToken);
                payloadLength = b29[7] | b29[6] >> 56 | b29[5] >> 48 | b29[4] >> 40 | b29[3] >> 32 | b29[2] >> 24 | b29[1] >> 16 | b29[0] >> 8 | 127;
            }

            if ((b1 & 128) == 128)
            {
                webSocketFrame.MaskingKey = await ReadHeaderAsync(4, cancellationToken);
            }

            if (payloadLength > 0)
            {
                webSocketFrame.Payload = new ArraySegment<byte>(new byte[payloadLength], 0, payloadLength);
                await ReadPayloadAsync(webSocketFrame.Payload, cancellationToken).ConfigureAwait(false);
            }

            webSocketFrame.Mask();
            return webSocketFrame;
        }


        private async Task ReadPayloadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            var effectiveCount = await _receiveStream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count, cancellationToken).ConfigureAwait(false);
            if (effectiveCount == 0 || effectiveCount != buffer.Count)
            {
                throw new TaskCanceledException();
            }
        }

        private async Task<byte[]> ReadHeaderAsync(int count, CancellationToken cancellationToken)
        {
            var effectiveCount = await _receiveStream.ReadAsync(_headBuffer, 0, count, cancellationToken).ConfigureAwait(false);
            if (effectiveCount == 0 || effectiveCount != count)
            {
                throw new TaskCanceledException();
            }

            return _headBuffer;
        }
    }
}
