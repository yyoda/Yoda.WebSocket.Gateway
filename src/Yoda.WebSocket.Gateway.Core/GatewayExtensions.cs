using System;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Yoda.WebSocket.Gateway.Core
{
    public static class GatewayExtensions
    {
        public static IApplicationBuilder UseWebSocketGateway(this IApplicationBuilder app, GatewayOptions options)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (options == null) throw new ArgumentNullException(nameof(options));

            app.UseWebSockets(options);
            app.UseMiddleware<GatewayMiddleware>(options);

            return app;
        }

        private const string RemoteHostKeyName = "X-Remote-Host";
        private const string ConnectionIdKeyName = "X-Connection-Id";

        public static GatewayClientConnection GetGatewayConnection(this HttpRequest request)
        {
            var hasRemoteHost = request.Headers.TryGetValue(RemoteHostKeyName, out var remoteHost);
            var hasConnectionId = request.Headers.TryGetValue(ConnectionIdKeyName, out var connectionId);

            return hasRemoteHost && hasConnectionId ? new GatewayClientConnection(remoteHost, connectionId) : null;
        }

        public static void SetGatewayConnectionHeader(this HttpRequestMessage request, string remoteHost, string connectionId)
        {
            request.Headers.Add(RemoteHostKeyName, remoteHost);
            request.Headers.Add(ConnectionIdKeyName, connectionId);
        }
    }
}