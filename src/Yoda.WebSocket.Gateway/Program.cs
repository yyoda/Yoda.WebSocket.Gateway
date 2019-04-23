using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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
                    var forwardUrl = configuration["FORWARD_URL"] ??
                                     throw new InvalidOperationException("FORWARD_URL does not exist.");

                    services.AddHttpClient("default")
                        .ConfigureHttpClient(client => client.BaseAddress = new Uri(forwardUrl))
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

                    var gatewayUrl = configuration["GATEWAY_URL"] ??
                                     throw new InvalidOperationException("GATEWAY_URL does not exist.");

                    var options = new GatewayOptions(gatewayUrl)
                    {
                        KeepAliveInterval = TimeSpan.FromMinutes(1),
                        ReceiveBufferSize = 6 * 1024,
                    };

                    app.UseWebSocketGateway(options);

                    app.UseRouter(router =>
                    {
                        var env = app.ApplicationServices.GetService<IHostingEnvironment>();

                        var settings = new JsonSerializerSettings
                        {
                            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
                        };

                        async Task Authenticate(HttpContext context, Func<HttpContext, Task> callback)
                        {
                            if (!env.IsProduction() || context.Request.Headers.ContainsKey("X-Forwarded-For"))
                            {
                                await callback(context);
                            }
                            else
                            {
                                context.Response.StatusCode = 401;
                                await context.Response.WriteAsync("Access Denied.");
                            }
                        }

                        router.MapGet("/status", context => Authenticate(context, current =>
                        {
                            current.Response.ContentType = "application/json";
                            return current.Response.WriteAsync(JsonConvert.SerializeObject(new GatewayMetrics {Options = options}, settings));
                        }));

                        router.MapGet("/env", context => Authenticate(context, current =>
                        {
                            current.Response.ContentType = "application/json";
                            return current.Response.WriteAsync(JsonConvert.SerializeObject(configuration.AsEnumerable(), settings));
                        }));
                    });
                })
                .Build()
                .Run();
        }
    }
}
