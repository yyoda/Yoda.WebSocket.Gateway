using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Yoda.WebSocket.Gateway.Core;

namespace Backend.Server.Controllers
{
    [ApiController]
    [Route("api/message")]
    public class MessageController : ControllerBase
    {
        private readonly IGatewayClient _client;
        private static readonly IDictionary<string, GatewayClientConnection> Connections = new ConcurrentDictionary<string, GatewayClientConnection>();

        public MessageController(IGatewayClient client) => _client = client;

        [HttpPost("text")]
        public IActionResult BroadcastText([FromBody] string message)
        {
            var connection = base.Request.GetGatewayConnection();
            Connections[connection.Id] = connection;
            _client.BroadcastMessage(Connections.Values.ToArray(), message);
            return Ok();
        }

        [HttpPost("binary")]
        public IActionResult BroadcastBinary([FromBody] byte[] message)
        {
            var connection = base.Request.GetGatewayConnection();
            Connections[connection.Id] = connection;
            _client.BroadcastMessage(Connections.Values.ToArray(), message);
            return Ok();
        }
    }
}
