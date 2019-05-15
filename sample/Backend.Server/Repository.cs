using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Yoda.WebSocket.Gateway.Core;

namespace Backend.Server
{
    public interface IRepository
    {
        Task SetConnectionAsync(string groupId, GatewayClientConnection connection);
        Task<GatewayClientConnection[]> GetConnectionAsync(string groupId);
    }

    public class Repository : IRepository
    {
        private readonly IDatabase _db;
        private readonly TimeSpan _expiry = TimeSpan.FromMinutes(1);
        private const string KeyPrefix = "WebSocket.Gateway.Connection";

        public Repository(string connectionString, int db = -1)
        {
            var redis = ConnectionMultiplexer.Connect(connectionString);
            _db = redis.GetDatabase(db);
        }

        public async Task SetConnectionAsync(string groupId, GatewayClientConnection connection)
        {
            var value = JsonConvert.SerializeObject(connection);
            await _db.SetAddAsync($"{KeyPrefix}:{groupId}", value);
            await _db.KeyExpireAsync($"{KeyPrefix}:{groupId}", _expiry, CommandFlags.FireAndForget);
        }

        public async Task<GatewayClientConnection[]> GetConnectionAsync(string groupId)
        {
            var redisValues = await _db.SetMembersAsync($"{KeyPrefix}:{groupId}") ?? new RedisValue[0];
            var stringValues = redisValues.Select(value => value.ToString()).ToArray();

            if (!stringValues.Any())
            {
                return new GatewayClientConnection[0];
            }

            var jsonValues = $"[{string.Join(',', stringValues)}]";

            return JsonConvert.DeserializeObject<GatewayClientConnection[]>(jsonValues);
        }
    }
}