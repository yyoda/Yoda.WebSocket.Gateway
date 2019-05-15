using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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

            services.AddSingleton(options);

            services.AddHttpClient(GatewayConstant.DefaultHttpClientName)
                .ConfigureHttpClient(client => client.BaseAddress = new Uri(options.ForwardUrl))
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    MaxConnectionsPerServer = 100,
                })
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

        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>() ?? new NullLoggerFactory();
            var logger = loggerFactory.CreateLogger(nameof(GatewayExtensions));

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                logger.LogError(GatewayConstant.OccurredUnhandledException, $"IsTerminating: {args.IsTerminating}");
                var id = AppDomain.CurrentDomain.Id;

                if (args.ExceptionObject is Exception e)
                {
                    logger.LogError(GatewayConstant.OccurredUnhandledException, e, $"IsTerminating: {args.IsTerminating}, Id: {id}");
                }
                else
                {
                    logger.LogError(GatewayConstant.OccurredUnhandledException, $"IsTerminating: {args.IsTerminating}, Id: {id}");
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.SetObserved();

                args.Exception.Handle(e =>
                {
                    logger.LogError(GatewayConstant.OccurredUnobservedTaskException, e, "Unobserved error in TaskScheduler");
                    return true;
                });
            };

            return app;
        }

        public static GatewayClientConnection GetGatewayConnection(this HttpRequest request)
        {
            var hasRemoteHost = request.Headers.TryGetValue(GatewayConstant.RemoteHostKeyName, out var remoteHost);
            var hasConnectionId = request.Headers.TryGetValue(GatewayConstant.ConnectionIdKeyName, out var connectionId);

            return hasRemoteHost && hasConnectionId ? new GatewayClientConnection(remoteHost, connectionId) : null;
        }

        internal static async Task AsUnauthorized(this HttpResponse response, string message = "Unauthorized", CancellationToken? cancellationToken = null)
        {
            response.StatusCode = 401;
            await response.WriteAsync(message, cancellationToken ?? CancellationToken.None);
        }

        internal static async Task AsBadRequest(this HttpResponse response, string message = "Bad Request", CancellationToken? cancellationToken = null)
        {
            response.StatusCode = 400;
            await response.WriteAsync(message, cancellationToken ?? CancellationToken.None);
        }

        internal static async Task AsGone(this HttpResponse response, string message = "Gone", CancellationToken? cancellationToken = null)
        {
            response.StatusCode = 410;
            await response.WriteAsync(message, cancellationToken ?? CancellationToken.None);
        }
    }
}