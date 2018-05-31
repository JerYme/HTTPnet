using System.IO;
using System.Threading.Tasks;
using HTTPnet.Core.Http;
using HTTPnet.Core.Http.Raw;
using System.Net;

namespace HTTPnet.Core.Pipeline.Handlers
{
    public class RequestBodyHandler : IHttpContextPipelineHandler
    {
        public async Task ProcessRequestAsync(HttpContextPipelineHandlerContext context)
        {
            var bodyLength = 0;
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            if (request.Headers.TryGetValue(HttpHeader.ContentLength, out var v))
            {
                bodyLength = int.Parse(v);
            }
            
            if (bodyLength == 0)
            {
                request.Body = new MemoryStream(0);
                return;
            }

            if (request.Headers.ValueEquals(HttpHeader.Expect, "100-Continue"))
            {
                var response = new RawHttpResponse
                {
                    Version = request.Version,
                    StatusCode = (int)HttpStatusCode.Continue
                };

                await httpContext.SessionHandler.ResponseWriter.WriteAsync(response, httpContext.ClientSession.CancellationToken);
            }

            while (httpContext.SessionHandler.RequestReader.BufferLength < bodyLength)
            {
                await httpContext.SessionHandler.RequestReader.FetchChunk(httpContext.ClientSession.CancellationToken);
            }

            request.Body = new MemoryStream(bodyLength);
            for (var i = 0; i < bodyLength; i++)
            {
                request.Body.WriteByte(httpContext.SessionHandler.RequestReader.DequeueFromBuffer());
            }
             
            request.Body.Position = 0;
        }

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context) => Task.CompletedTask;
    }
}
