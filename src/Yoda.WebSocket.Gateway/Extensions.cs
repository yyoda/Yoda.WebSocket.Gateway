using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yoda.WebSocket.Gateway.Core;

namespace Yoda.WebSocket.Gateway
{
    public static class Extensions
    {
        public static IApplicationBuilder UseErrorHandler(this IApplicationBuilder app)
        {
            var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>() ?? new NullLoggerFactory();
            var logger = loggerFactory.CreateLogger(nameof(Extensions));

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
                    logger.LogError(GatewayLogEvent.UnobservedTaskError, "Unobserved error in TaskScheduler", e);
                    return true;
                });
            };

            return app;
        }
    }
}