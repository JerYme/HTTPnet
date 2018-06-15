using System;
using System.IO;

namespace HTTPnet.Core.WebSockets.Protocol
{
    public struct PayloadStream
    {
        public readonly Stream DataStream;
        public readonly int BufferSize;

        public PayloadStream(Stream dataStream, int bufferSize = 8192)
        {
            if (dataStream == null) throw new ArgumentNullException(nameof(dataStream));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            DataStream = dataStream;
            BufferSize = bufferSize;
        }
    }

    public class WebSocketFrame
    {
        public bool Fin { get; set; } = true;
        public WebSocketOpcode Opcode { get; set; } = WebSocketOpcode.Binary;
        public byte[] MaskingKey { get; set; }

        public ArraySegment<byte> Payload { get; set; }
        public PayloadStream PayloadStream { get; set; }

        public void Mask()
        {
            var maskingKey = MaskingKey;
            if (maskingKey == null) return;
            var segment = Payload;
            var bytesArray = segment.Array;

            var mi = 0;
            for (var i = Payload.Offset; i < Payload.Count; i++)
            {
                bytesArray[i] = (byte)(bytesArray[i] ^ maskingKey[mi]);
                if (mi == 3) mi = 0;
                else ++mi;
            }
        }
    }
}
