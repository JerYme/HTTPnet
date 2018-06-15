using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace HTTPnet.Core.Http.Raw
{
    public class RawHttpResponse
    {
        public string Version { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Stream Body { get; set; }

        public string BuildHttpHeader()
        {
            var buffer = new StringBuilder();
            buffer.Append(Version);
            buffer.Append(" ");
            buffer.Append((int)StatusCode);
            buffer.Append(" ");
            buffer.Append(ReasonPhrase);
            buffer.AppendLine();

            foreach (var header in Headers)
            {
                buffer.Append(header.Key);
                buffer.Append(":");
                buffer.Append(header.Value);
                buffer.AppendLine();
            }

            buffer.AppendLine();

            return buffer.ToString();

        }
    }
}