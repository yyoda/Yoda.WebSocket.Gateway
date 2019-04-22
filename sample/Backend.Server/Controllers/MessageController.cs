using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Yoda.WebSocket.Gateway.Core;

namespace Backend.Server.Controllers
{
    [ApiController]
    [Route("api/message")]
    public class MessageController : ControllerBase
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        private static readonly HashSet<string> ConnectionIds = new HashSet<string>();

        [HttpPost("text/{id}")]
        public IActionResult PostText(string id, [FromBody] string message)
        {
            ConnectionIds.Add(id);
            var content = new StringContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            Broadcast(content);
            return Ok();
        }

        [HttpPost("binary/{id}")]
        public IActionResult PostBinary(string id, [FromBody] byte[] message)
        {
            ConnectionIds.Add(id);
            var content = new ByteArrayContent(message);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            Broadcast(content);
            return Ok();
        }

        private static void Broadcast(HttpContent content)
        {
            foreach (var connectionId in ConnectionIds)
            {
                HttpClient.PostAsync($"cb/{connectionId}", content).ConfigureAwait(false);
            }
        }
    }
}
