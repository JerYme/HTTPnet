namespace HTTPnet.Core.Http
{
    public class HttpClientOptions
    {
        public static HttpClientOptions Default => new HttpClientOptions();

        public int Port { get; set; } = 80;

        public bool NoDelay { get; set; } = true;

        public int Backlog { get; set; } = 10;

        public int SendBufferSize { get; set; } = 81920;

        public int ReceiveChunkSize { get; set; } = 8 * 1024; // 8 KB
    }
}