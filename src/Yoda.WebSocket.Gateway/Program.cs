using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Yoda.WebSocket.Gateway.Core;

namespace Yoda.WebSocket.Gateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configuration)
                .ConfigureServices((context, services) =>
                {
                    var gatewayUrl = AwsUtility.GetContainerHostUrlAsync(context.Configuration).Result;
                    var forwardUrl = context.Configuration["FORWARD_URL"];

                    services.AddWebSocketGateway(new GatewayOptions(gatewayUrl, forwardUrl)
                    {
                        KeepAliveInterval = TimeSpan.FromMinutes(1),
                        ReceiveBufferSize = 6 * 1024,
                        HttpHandlerLifetime = TimeSpan.FromMinutes(5),
                    });

                    services.AddRouting();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseErrorHandler();
                    app.UseWebSocketGateway();
                    app.UseRouter(router =>
                    {
                        router.StatusApi();
                        router.EnvApi();
                    });
                })
                .Build()
                .Run();
        }
    }
}
