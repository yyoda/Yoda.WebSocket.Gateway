using System;
using Microsoft.AspNetCore.Builder;

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
    }
}