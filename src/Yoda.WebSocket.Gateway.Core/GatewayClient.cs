using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Yoda.WebSocket.Gateway.Core
{
    public interface IGatewayClient
    {
        void BroadcastMessage(GatewayClientConnection[] connections, string message);
        void BroadcastMessage(GatewayClientConnection[] connections, byte[] message);
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

        public void BroadcastMessage(GatewayClientConnection[] connections, string message)
        {
            var content = new StringContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

            foreach (var connectionId in connections)
            {
                RequestAsync(connectionId, content).ConfigureAwait(false);
            }
        }

        public void BroadcastMessage(GatewayClientConnection[] connections, byte[] message)
        {
            var content = new ByteArrayContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            foreach (var connectionId in connections)
            {
                RequestAsync(connectionId, content).ConfigureAwait(false);
            }
        }

        private async Task RequestAsync(GatewayClientConnection connection, HttpContent content)
        {
            var uri = $"{connection.RemoteHost}cb/{connection.Id}";

            try
            {
                var response = await Client.PostAsync(uri, content);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    Logger.LogError(GatewayConstant.AbortedHttpRequest, $"uri: {uri}, status: {response.StatusCode}, connection: {connection}, content: {body}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(GatewayConstant.AbortedHttpRequest, e, $"uri: {uri}");
            }
        }
    }

    public class GatewayClientConnection
    {
        public GatewayClientConnection(string remoteHost, string id)
        {
            RemoteHost = remoteHost;
            Id = id;
        }

        public string RemoteHost { get; }
        public string Id { get; }

        public override string ToString() => $"remoteHost: {RemoteHost}, id: {Id}";
    }
}