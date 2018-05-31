using System;
using System.Threading.Tasks;
using HTTPnet.Core.Http;

namespace HTTPnet.Core
{
    public interface IHttpClient : IDisposable
    {
        IHttpRequestHandler RequestHandler { get; set; }

        Task StartAsync(HttpClientOptions options);
        Task StopAsync();
    }
}