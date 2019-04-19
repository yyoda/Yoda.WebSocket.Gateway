using System.Collections.Concurrent;

namespace Yoda.WebSocket.Gateway
{
    public class GatewayConnection
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _sockets;

        private GatewayConnection() =>
            _sockets = new ConcurrentDictionary<string, System.Net.WebSockets.WebSocket>();

        public static GatewayConnection Instance { get; } = new GatewayConnection();

        internal System.Net.WebSockets.WebSocket GetSocket(string connectionId) =>
            _sockets.TryGetValue(connectionId, out var socket) ? socket : null;

        internal void SetSocket(string connectionId, System.Net.WebSockets.WebSocket socket) =>
            _sockets[connectionId] = socket;

        internal void RemoveSocket(string connectionId) =>
            _sockets.TryRemove(connectionId, out _);

        public int Count => _sockets.Count;
    }
}