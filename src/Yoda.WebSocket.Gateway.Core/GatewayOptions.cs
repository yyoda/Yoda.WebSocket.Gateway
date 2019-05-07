using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Yoda.WebSocket.Gateway.Core
{
    public class GatewayOptions : WebSocketOptions
    {
        public GatewayOptions(string gatewayUrl, string forwardUrl)
        {
            if (string.IsNullOrWhiteSpace(gatewayUrl)) throw new ArgumentNullException(nameof(gatewayUrl));
            if (string.IsNullOrWhiteSpace(forwardUrl)) throw new ArgumentNullException(nameof(forwardUrl));

            GatewayUrl = gatewayUrl;
            ForwardUrl = forwardUrl;
        }

        public string WebSocketEndpoint { get; set; } = "/ws";
        public string CallbackEndpoint { get; set; } = "/cb";
        public string GatewayUrl { get; }
        public string ForwardUrl { get; }
        public TimeSpan HttpHandlerLifetime { get; set; } = TimeSpan.FromMinutes(5);

        [IgnoreDataMember]
        public Func<HttpRequest, WebSocketMessageType> WebSocketMessageTypeSelector { get; set; } = request =>
        {
            if (request.ContentType.Contains("application/json") || request.ContentType.Contains("text/plain"))
            {
                return WebSocketMessageType.Text;
            }

            return WebSocketMessageType.Binary;
        };

        [IgnoreDataMember]
        public Func<byte[], WebSocketMessageType, HttpContent> HttpContentFactory { get; set; } = (data, type) =>
        {
            switch (type)
            {
                case WebSocketMessageType.Binary:
                    var content = new ByteArrayContent(data);
                    content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                    return content;
                case WebSocketMessageType.Text:
                    var text = Encoding.UTF8.GetString(data);
                    return new StringContent(text, Encoding.UTF8, "text/plain");
                default:
                    return null;
            }
        };
    }
}