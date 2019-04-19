using System.Net;
using Backend.Server.Formatters;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Server
{
    public class Program
    {
        public static void Main(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureKestrel((context, options) =>
                {
                    options.Listen(IPAddress.Loopback, 5001);
                })
                .ConfigureServices(services =>
                {
                    services.AddMvc(options =>
                    {
                        options.InputFormatters.Insert(0, new TextPlainInputFormatter());
                        options.InputFormatters.Insert(1, new BinaryInputFormatter());
                    });
                })
                .Configure(app =>
                {
                    app.UseMvc();
                })
                .Build()
                .Run();
    }
}
