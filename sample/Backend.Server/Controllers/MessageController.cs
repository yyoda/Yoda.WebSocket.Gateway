using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Yoda.WebSocket.Gateway.Core;

namespace Backend.Server.Controllers
{
    [ApiController]
    [Route("api/message")]
    public class MessageController : ControllerBase
    {
        private readonly IGatewayClient _client;
        private readonly IRepository _repository;
        private const int GroupSize = 5;

        public MessageController(IGatewayClient client, IRepository repository)
        {
            _client = client;
            _repository = repository;
        }

        [HttpPost("text")]
        public async Task BroadcastText([FromBody] string message)
        {
            var connection = Request.GetGatewayConnection();
            var connectionId = int.Parse(connection.Id);
            var groupId = connectionId / GroupSize;
            await _repository.SetConnectionAsync(groupId.ToString(), connection);
            var connections = await _repository.GetConnectionAsync(groupId.ToString());

            _client.BroadcastMessage(connections, message);
        }

        [HttpPost("binary")]
        public async Task BroadcastBinary([FromBody] byte[] message)
        {
            var connection = Request.GetGatewayConnection();
            var connectionId = int.Parse(connection.Id);
            var groupId = connectionId / GroupSize;
            await _repository.SetConnectionAsync(groupId.ToString(), connection);
            var connections = await _repository.GetConnectionAsync(groupId.ToString());

            _client.BroadcastMessage(connections, message);
        }
    }
}
