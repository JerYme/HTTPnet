﻿using System;
using System.Text;
using System.Threading.Tasks;
using HTTPnet.Core.Http;
using HTTPnet.Core.WebSockets;
using System.Net;

namespace HTTPnet.Core.Pipeline.Handlers
{
    public abstract class WebSocketUpgradeRequestHandler : IHttpContextPipelineHandler
    {
        protected abstract Task ProcessRequestAsync(HttpContextPipelineHandlerContext context);

        Task IHttpContextPipelineHandler.ProcessRequestAsync(HttpContextPipelineHandlerContext context)
        {
            var isWebSocketRequest = context.HttpContext.Request.Headers.ValueEquals(HttpHeader.Upgrade, "websocket");
            if (!isWebSocketRequest)
            {
                return Task.FromResult(0);
            }

            return ProcessRequestAsync(context);
        }

        Task IHttpContextPipelineHandler.ProcessResponseAsync(HttpContextPipelineHandlerContext context) => Task.FromResult(0);
    }

    public class WebSocketAcceptRequestHandler : WebSocketUpgradeRequestHandler
    {
        private readonly Action<WebSocketSession> _sessionCreated;
        private readonly Func<byte[], byte[]> _sha1Computor;

        public WebSocketAcceptRequestHandler(Func<byte[], byte[]> sha1Computor, Action<WebSocketSession> sessionCreated = null)
        {
            _sha1Computor = sha1Computor ?? throw new ArgumentNullException(nameof(sha1Computor));
            _sessionCreated = sessionCreated;
        }

        protected override Task ProcessRequestAsync(HttpContextPipelineHandlerContext context)
        {
            var webSocketKey = context.HttpContext.Request.Headers[HttpHeader.SecWebSocketKey];
            var responseKey = webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            var responseKeyBuffer = Encoding.UTF8.GetBytes(responseKey);

            var hash = _sha1Computor(responseKeyBuffer);
            var secWebSocketAccept = Convert.ToBase64String(hash);

            context.HttpContext.Response.StatusCode = HttpStatusCode.SwitchingProtocols;
            context.HttpContext.Response.Headers[HttpHeader.Connection] = "Upgrade";
            context.HttpContext.Response.Headers[HttpHeader.Upgrade] = "websocket";
            context.HttpContext.Response.Headers[HttpHeader.SecWebSocketAccept] = secWebSocketAccept;

            var webSocketSession = new WebSocketSession(context.HttpContext.ClientSession);
            context.HttpContext.ClientSession.SwitchProtocol(webSocketSession);

            _sessionCreated?.Invoke(webSocketSession);

            context.BreakPipeline = true;

            return Task.FromResult(0);
        }
    }
}
