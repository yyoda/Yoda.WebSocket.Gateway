using Microsoft.Extensions.Logging;

namespace Yoda.WebSocket.Gateway.Core
{
    public class GatewayConstant
    {
        public static EventId OccurredUnhandledException = new EventId(1, nameof(OccurredUnhandledException));
        public static EventId OccurredUnobservedTaskException = new EventId(2, nameof(OccurredUnobservedTaskException));
        public static EventId FailedWebSocketHandshake = new EventId(3, nameof(FailedWebSocketHandshake));
        public static EventId AbortedWebSocketConnection = new EventId(4, nameof(AbortedWebSocketConnection));
        public static EventId AbortedHttpRequest = new EventId(5, nameof(AbortedHttpRequest));
        public static EventId InvalidWebSocketMessageType = new EventId(6, nameof(InvalidWebSocketMessageType));

        public const string DefaultHttpClientName = "default";
        public const string RemoteHostKeyName = "X-Remote-Host";
        public const string ConnectionIdKeyName = "X-Connection-Id";
    }
}
