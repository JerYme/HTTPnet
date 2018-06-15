using System.Globalization;
using System.Threading.Tasks;
using HTTPnet.Core.Http;

namespace HTTPnet.Core.Pipeline.Handlers
{
    public class ResponseBodyLengthHandler : IHttpContextPipelineHandler
    {
        public Task ProcessRequestAsync(HttpContextPipelineHandlerContext context) => Task.FromResult(0);

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context)
        {
            context.HttpContext.Response.Headers[HttpHeader.ContentLength] = (context.HttpContext.Response.Body?.Length ?? 0).ToString(CultureInfo.InvariantCulture);
            return Task.FromResult(0);
        }
    }
}
