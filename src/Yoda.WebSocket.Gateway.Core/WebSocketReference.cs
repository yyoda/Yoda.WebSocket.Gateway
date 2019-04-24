using System.Collections.Concurrent;

namespace Yoda.WebSocket.Gateway.Core
{
    public class WebSocketReference
    {
        private readonly ConcurrentDictionary<string, System.Net.WebSockets.WebSocket> _sockets;

        private WebSocketReference() =>
            _sockets = new ConcurrentDictionary<string, System.Net.WebSockets.WebSocket>();

        public static WebSocketReference Instance { get; } = new WebSocketReference();

        internal System.Net.WebSockets.WebSocket GetSocket(string connectionId) =>
            _sockets.TryGetValue(connectionId, out var socket) ? socket : null;

        internal void SetSocket(string connectionId, System.Net.WebSockets.WebSocket socket) =>
            _sockets[connectionId] = socket;

        internal void RemoveSocket(string connectionId) =>
            _sockets.TryRemove(connectionId, out _);

        public int Count => _sockets.Count;
    }
}