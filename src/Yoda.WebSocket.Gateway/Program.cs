using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
                    var endpoint = configuration["FORWARD_ENDPOINT"] ??
                                          throw new InvalidOperationException("FORWARD_ENDPOINT does not exist.");

                    services.AddHttpClient("default")
                        .ConfigureHttpClient(client => client.BaseAddress = new Uri(endpoint))
                        .ConfigurePrimaryHttpMessageHandler<SocketsHttpHandler>()
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                    services.AddRouting();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseErrorHandler();


                    var options = new GatewayOptions
                    {
                        KeepAliveInterval = TimeSpan.FromMinutes(1),
                        ReceiveBufferSize = 6 * 1024,
                    };

                    app.UseWebSocketGateway(options);

                    app.UseRouter(router =>
                    {
                        var settings = new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                        };

                        router.MapGet("/status", async (request, response, route) =>
                        {
                            response.ContentType = "application/json";
                            await response.WriteAsync(JsonConvert.SerializeObject(new GatewayMetrics { Options = options, }, settings));
                        });

                        router.MapGet("/env", async (request, response, route) =>
                        {
                            response.ContentType = "application/json";
                            await response.WriteAsync(JsonConvert.SerializeObject(configuration.AsEnumerable(), settings));
                        });
                    });
                })
                .Build()
                .Run();
        }
    }
}
