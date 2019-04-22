using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
                .UseUrls("http://*:5000/")
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseErrorHandler();

                    var options = new GatewayOptions("http://localhost:5001/api/message")
                    {
                        KeepAliveInterval = TimeSpan.FromMinutes(1),
                        ReceiveBufferSize = 6 * 1024,
                        LoggerFactory = app.ApplicationServices.GetService<ILoggerFactory>() ?? new NullLoggerFactory(),
                    };

                    app.UseWebSocketGateway(options);

                    app.UseRouter(router =>
                    {
                        router.MapGet("/status", async (request, response, route) =>
                        {
                            response.StatusCode = 200;
                            response.ContentType = "application/json";
                            var memoryCache = app.ApplicationServices.GetService<IMemoryCache>() as MemoryCache;
                            var settings = new JsonSerializerSettings
                            {
                                ContractResolver = new DefaultContractResolver
                                {
                                    NamingStrategy = new CamelCaseNamingStrategy()
                                }
                            };
                            var metrics = new GatewayMetrics
                            {
                                MemoryCacheCount = memoryCache?.Count ?? 0,
                                Options = options,
                            };
                            await response.WriteAsync(JsonConvert.SerializeObject(metrics, settings));
                        });
                    });
                })
                .Build()
                .Run();
        }
    }
}
