using HTTPnet.Core;
using HTTPnet.Implementations;

namespace HTTPnet
{
    public class HttpClientFactory
    {
        public HttpClient CreateHttpClient() => new HttpClient(o => new ClientSocketWrapper(o));
    }
}