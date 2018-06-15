﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using HTTPnet.Core.Diagnostics;

namespace HTTPnet.Core.Pipeline.Handlers
{
    public class TraceHandler : IHttpContextPipelineHandler
    {
        public Task ProcessRequestAsync(HttpContextPipelineHandlerContext context)
        {
            var body = "<no body>";

            if (context.HttpContext.Request.Body != null)
            {
                using (var streamReader = new StreamReaderPeekable(context.HttpContext.Request.Body, Encoding.UTF8, false, 1024, true))
                {
                    body = streamReader.ReadToEnd();
                }

                context.HttpContext.Request.Body.Position = 0;
            }

            HttpNetTrace.Verbose(nameof(TraceHandler), context.HttpContext.Request.Method + " " + context.HttpContext.Request.Uri + " " + body);
            return Task.FromResult(0);
        }

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context)
        {
            HttpNetTrace.Verbose(nameof(TraceHandler), context.HttpContext.Response.StatusCode + " " + context.HttpContext.Response.ReasonPhrase);
            return Task.FromResult(0);
        }
    }
}
