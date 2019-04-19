using System;
using Microsoft.AspNetCore.Builder;

namespace Yoda.WebSocket.Gateway.Core
{
    public static class GatewayExtensions
    {
        public static IApplicationBuilder UseWebSocketGateway(this IApplicationBuilder app, GatewayOptions options = null)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var configuredOptions = options ?? new GatewayOptions();

            app.UseWebSockets(options);
            app.UseMiddleware<GatewayMiddleware>(configuredOptions);

            return app;
        }
    }
}