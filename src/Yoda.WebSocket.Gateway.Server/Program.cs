using System;
using System.IO;
using System.Threading.Tasks;
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

namespace Yoda.WebSocket.Gateway.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()
                .Build();

            await WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configuration)
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();

                    if (hostingContext.HostingEnvironment.IsDevelopment())
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                })
                .Configure(app =>
                {
                    var options = new GatewayOptions
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
                            var text = JsonConvert.SerializeObject(new GatewayMetrics
                            {
                                MemoryCacheCount = memoryCache?.Count ?? 0,
                                Options = options,
                            }, settings);
                            await response.WriteAsync(text);
                        });
                    });
                })
                .Build()
                .RunAsync();
        }
    }
}
