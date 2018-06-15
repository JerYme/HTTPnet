﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HTTPnet.Core.Http.Raw
{
    public class RawHttpResponseWriter
    {
        private readonly Stream _sendStream;
        private readonly HttpServerOptions _options;

        public RawHttpResponseWriter(Stream sendStream, HttpServerOptions options)
        {
            _sendStream = sendStream ?? throw new ArgumentNullException(nameof(sendStream));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task WriteAsync(RawHttpResponse response, CancellationToken cancellationToken)
        {
            if (response == null) throw new ArgumentNullException(nameof(response));
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));

            var s = response.BuildHttpHeader();
            var headerBytes = Encoding.UTF8.GetBytes(s);

            await _sendStream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken).ConfigureAwait(false);

            if (response.Body != null && response.Body.Length > 0)
            {
                response.Body.Position = 0;
                await response.Body.CopyToAsync(_sendStream, _options.SendBufferSize, cancellationToken).ConfigureAwait(false);
            }

            await _sendStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

    }
}
