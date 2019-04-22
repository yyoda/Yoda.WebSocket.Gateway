using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Yoda.WebSocket.Gateway.Core
{
    public interface IGatewayClient
    {
        void BroadcastMessage(string[] connectionIds, string message);
        void BroadcastMessage(string[] connectionIds, byte[] message);
    }

    public class GatewayClient : IGatewayClient
    {
        protected HttpClient Client { get; }
        protected ILogger Logger { get; }

        public GatewayClient(HttpClient client, ILoggerFactory loggerFactory)
        {
            Client = client;
            Logger = loggerFactory.CreateLogger<GatewayClient>();
        }

        public void BroadcastMessage(string[] connectionIds, string message)
        {
            var content = new StringContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

            foreach (var connectionId in connectionIds)
            {
                RequestAsync(connectionId, content).ConfigureAwait(false);
            }
        }

        public void BroadcastMessage(string[] connectionIds, byte[] message)
        {
            var content = new ByteArrayContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            foreach (var connectionId in connectionIds)
            {
                RequestAsync(connectionId, content).ConfigureAwait(false);
            }
        }

        private async Task RequestAsync(string connectionId, HttpContent content)
        {
            var response = await Client.PostAsync($"cb/{connectionId}", content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Logger.LogError(GatewayLogEvent.HttpRequestError, $"status: {response.StatusCode}, id: {connectionId}, content: {body}");
            }
        }
    }
}