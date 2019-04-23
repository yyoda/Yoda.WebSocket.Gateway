using System;
using System.IO;
using System.Net.Http;
using Backend.Server.Formatters;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Yoda.WebSocket.Gateway.Core;

namespace Backend.Server
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
                .ConfigureServices(services =>
                {
                    services.AddHttpClient<IGatewayClient, GatewayClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler())
                        .SetHandlerLifetime(TimeSpan.FromMinutes(5));

                    services.AddMvc(options =>
                    {
                        options.InputFormatters.Insert(0, new TextPlainInputFormatter());
                        options.InputFormatters.Insert(1, new BinaryInputFormatter());
                    });
                })
                .Configure(app => { app.UseMvc(); })
                .Build()
                .Run();
        }
    }
}
