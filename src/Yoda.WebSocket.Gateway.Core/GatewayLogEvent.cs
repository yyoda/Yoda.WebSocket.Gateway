using Microsoft.Extensions.Logging;

namespace Yoda.WebSocket.Gateway.Core
{
    public class GatewayLogEvent
    {
        public static EventId UnhandledError = new EventId(1, nameof(UnhandledError));
        public static EventId UnobservedTaskError = new EventId(2, nameof(UnobservedTaskError));
        public static EventId WebSocketHandshakeError = new EventId(3, nameof(WebSocketHandshakeError));
        public static EventId WebSocketConnectionError = new EventId(4, nameof(WebSocketConnectionError));
        public static EventId ApplicationEndpointError = new EventId(5, nameof(ApplicationEndpointError));
        public static EventId InvalidWebSocketMessageType = new EventId(6, nameof(InvalidWebSocketMessageType));
    }
}
