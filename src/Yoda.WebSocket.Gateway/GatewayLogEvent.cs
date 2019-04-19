using Microsoft.Extensions.Logging;

namespace Yoda.WebSocket.Gateway
{
    public class GatewayLogEvent
    {
        public static EventId ApplicationEndpointError = new EventId(1, nameof(ApplicationEndpointError));
        public static EventId InvalidWebSocketMessageType = new EventId(2, nameof(InvalidWebSocketMessageType));
    }
}
