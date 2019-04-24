using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Yoda.WebSocket.Gateway.Core
{
    public static class GatewayExtensions
    {
        public static IServiceCollection AddWebSocketGateway(this IServiceCollection services, GatewayOptions options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton(service => options);

            services.AddHttpClient("default")
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(options.ForwardUrl))
                .ConfigurePrimaryHttpMessageHandler<SocketsHttpHandler>()
                .SetHandlerLifetime(options.HttpHandlerLifetime);

            return services;
        }

        public static IApplicationBuilder UseWebSocketGateway(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            var options = app.ApplicationServices.GetRequiredService(typeof(GatewayOptions));

            app.UseWebSockets((WebSocketOptions) options);
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

        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>() ?? new NullLoggerFactory();
            var logger = loggerFactory.CreateLogger(nameof(GatewayExtensions));

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                logger.LogError(GatewayLogEvent.UnhandledError, $"IsTerminating: {args.IsTerminating}");
                var id = AppDomain.CurrentDomain.Id;

                if (args.ExceptionObject is Exception e)
                {
                    logger.LogError(GatewayLogEvent.UnhandledError, e, $"IsTerminating: {args.IsTerminating}, Id: {id}");
                }
                else
                {
                    logger.LogError(GatewayLogEvent.UnhandledError, $"IsTerminating: {args.IsTerminating}, Id: {id}");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.SetObserved();

                args.Exception.Handle(e =>
                {
                    logger.LogError(GatewayLogEvent.UnobservedTaskError, e, "Unobserved error in TaskScheduler");
                    return true;
                });
            };

            return app;
        }
    }
}