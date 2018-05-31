using System.Globalization;
using System.Threading.Tasks;
using HTTPnet.Core.Http;

namespace HTTPnet.Core.Pipeline.Handlers
{
    public class ResponseBodyLengthHandler : IHttpContextPipelineHandler
    {
        public Task ProcessRequestAsync(HttpContextPipelineHandlerContext context) => Task.CompletedTask;

        public Task ProcessResponseAsync(HttpContextPipelineHandlerContext context)
        {
            context.HttpContext.Response.Headers[HttpHeader.ContentLength] 
                = context.HttpContext.Response.Body?.Length.ToString(CultureInfo.InvariantCulture) ?? "0";
            return Task.CompletedTask;
        }
    }
}
