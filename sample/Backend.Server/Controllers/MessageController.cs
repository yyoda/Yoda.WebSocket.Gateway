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
        private static readonly HashSet<string> ConnectionIds = new HashSet<string>();

        public MessageController(IGatewayClient client) => _client = client;

        [HttpPost("text/{id}")]
        public IActionResult PostText(string id, [FromBody] string message)
        {
            ConnectionIds.Add(id);
            _client.BroadcastMessage(ConnectionIds.ToArray(), message);
            return Ok();
        }

        [HttpPost("binary/{id}")]
        public IActionResult PostBinary(string id, [FromBody] byte[] message)
        {
            ConnectionIds.Add(id);
            _client.BroadcastMessage(ConnectionIds.ToArray(), message);
            return Ok();
        }
    }
}
