using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HTTPnet.Core.Exceptions;

namespace HTTPnet.Core.Http.Raw
{
    public sealed class RawHttpRequestReader
    {
        private readonly Stream _receiveStream;
        private readonly StreamReaderPeekable _streamReaderPeekable;
        private readonly List<Stream> _receiveStreams;
        private readonly StreamOfStreams _streamOfStreams;
        private readonly byte[] _receiveBuffer;
        private readonly byte[] _receiveBufferTail;

        public RawHttpRequestReader(Stream receiveStream, HttpServerOptions options)
        {
            _receiveStream = receiveStream ?? throw new ArgumentNullException(nameof(receiveStream));
            if (options == null) throw new ArgumentNullException(nameof(options));
            _receiveBuffer = new byte[options.ReceiveChunkSize];
            _receiveBufferTail = new byte[options.ReceiveChunkSize];
            _receiveStreams = new List<Stream> { Stream.Null, _receiveStream };
            _streamOfStreams = new StreamOfStreams(_receiveStreams);
            _streamReaderPeekable = new StreamReaderPeekable(_streamOfStreams, Encoding.UTF8, false, options.ReceiveChunkSize, true);
        }

        public async Task<Stream> FetchContent(int contentLength, CancellationToken cancellationToken)
        {
            var buffer = _streamReaderPeekable.GetBytesFromCharBuffer();
            _streamReaderPeekable.DiscardBufferedData();

            _receiveStreams[0] = new ArraySegmentStream(buffer);
            _streamOfStreams.Reset();

            if (contentLength == 0) return Stream.Null;

            var body = contentLength > 0 ? new MemoryStream(contentLength) : new MemoryStream();

            int read = 0;
            while (true)
            {
                var r = await _streamOfStreams.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, cancellationToken);
                read += r;

                if (r == 0 || read == contentLength)
                {
                    if (contentLength == -1 || read == contentLength)
                    {
                        _receiveStreams[0] = Stream.Null;
                        _streamOfStreams.Reset();
                        body.Position = 0;
                        return body;
                    }
                    throw new HttpRequestInvalidException();
                }

                body.Write(_receiveBuffer, 0, r);
                if (contentLength == -1) continue;

                if (read > contentLength)
                {
                    var tailLength = read - contentLength;
                    Buffer.BlockCopy(_receiveBuffer, _receiveBuffer.Length - tailLength, _receiveBufferTail, 0, tailLength);
                    _receiveStreams[0] = new ArraySegmentStream(new ArraySegment<byte>(_receiveBufferTail, 0, tailLength));
                    _streamOfStreams.Reset();
                }
            }
        }

        public async Task<RawHttpRequest> ReadAsync(CancellationToken cancellationToken)
        {
            var statusLine = await _streamReaderPeekable.ReadLineAsync();
            var statusLineItems = statusLine.Split(' ');

            if (statusLineItems.Length != 3)
            {
                throw new HttpRequestInvalidException();
            }

            var headers = await ParseHeaders();

            var request = new RawHttpRequest
            {
                Method = statusLineItems[0].ToUpperInvariant(),
                Uri = statusLineItems[1],
                Version = statusLineItems[2].ToUpperInvariant(),
                Headers = headers
            };

            return request;
        }

        private async Task<Dictionary<string, string>> ParseHeaders()
        {
            var headers = new Dictionary<string, string>();
            while (true)
            {
                var line = await _streamReaderPeekable.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) return headers;
                var header = ParseHeader(line);
                headers.Add(header.Key, header.Value);
            }
        }

        private static KeyValuePair<string, string> ParseHeader(string source)
        {
            var delimiterIndex = source.IndexOf(':');
            if (delimiterIndex == -1)
            {
                return new KeyValuePair<string, string>(source, null);
            }

            var name = source.Substring(0, delimiterIndex).Trim();
            var value = source.Substring(delimiterIndex + 1).Trim();

            return new KeyValuePair<string, string>(name, value);
        }

    }
}