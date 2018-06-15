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
            var httpContext = context.HttpContext;
            var request = httpContext.Request;
            var sessionHandler = httpContext.SessionHandler;
            var cancellationToken = httpContext.ClientSession.CancellationToken;

            var contentLength = -1;
            if (request.Headers.TryGetValue(HttpHeader.ContentLength, out var v))
            {
                contentLength = int.Parse(v);
            }

            if (contentLength == 0)
            {
                request.Body = Stream.Null;
                return;
            }

            if (request.Headers.ValueEquals(HttpHeader.Expect, "100-Continue"))
            {
                var response = new RawHttpResponse
                {
                    Version = request.Version,
                    StatusCode = HttpStatusCode.Continue
                };

                await sessionHandler.ResponseWriter.WriteAsync(response, cancellationToken);
            }

           var bodyStream = await sessionHandler.RequestReader.FetchContent(contentLength, cancellationToken);
            request.Body = bodyStream;
        }

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context)
        {
            return Task.FromResult(0);
        }
    }
}
