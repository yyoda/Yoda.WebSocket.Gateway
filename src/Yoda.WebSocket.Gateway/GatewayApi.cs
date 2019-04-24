using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Yoda.WebSocket.Gateway.Core;

namespace Yoda.WebSocket.Gateway
{
    public static class GatewayApi
    {
        #region Helpers.

        private static async Task WriteJsonAsync<T>(this HttpResponse response, T model)
            where T : class
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
            };
            response.ContentType = "application/json";
            var json = JsonConvert.SerializeObject(model, settings);
            await response.WriteAsync(json);
        }

        private static async Task Hide(HttpContext context, IHostingEnvironment env, Func<Task> callback)
        {
            if (env.IsProduction() && context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Access Denied.");
            }
            else
            {
                await callback();
            }
        }

        #endregion

        public static IRouteBuilder StatusApi(this IRouteBuilder router)
        {
            var env = router.ServiceProvider.GetService<IHostingEnvironment>();
            var opt = router.ServiceProvider.GetService<GatewayOptions>();

            return router.MapGet("/status", context => Hide(context, env, async () =>
            {
                await context.Response.WriteJsonAsync(new GatewayStatus {Options = opt});
            }));
        }

        public static IRouteBuilder EnvApi(this IRouteBuilder router)
        {
            var env = router.ServiceProvider.GetService<IHostingEnvironment>();
            var cfg = router.ServiceProvider.GetService<IConfiguration>().AsEnumerable();

            return router.MapGet("/env", context => Hide(context, env, async () =>
            {
                await context.Response.WriteJsonAsync(cfg);
            }));
        }
    }
}