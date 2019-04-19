using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Yoda.WebSocket.Gateway.Core
{
    internal class GatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly GatewayOptions _options;
        private readonly ILogger _logger;
        private readonly HttpClient _http;

        public GatewayMiddleware(RequestDelegate next, GatewayOptions options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = _options.LoggerFactory.CreateLogger(nameof(GatewayMiddleware));
            _http = new HttpClient(_options.HttpMessageHandler);
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var cancellation = new CancellationTokenSource();

            if (context.Request.Path.StartsWithSegments(_options.WebSocketEndpoint))
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await HandleWebSocketRequestAsync(context, cancellation.Token);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid WebSocket protocol.", cancellation.Token);
                }
            }
            else if (context.Request.Path.StartsWithSegments(_options.CallbackEndpoint))
            {
                await HandleCallbackRequestAsync(context, cancellation.Token);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleWebSocketRequestAsync(HttpContext context, CancellationToken cancellationToken)
        {
            var connectionId = context.Request.Path.Value.Replace($"{_options.WebSocketEndpoint}/", "");
            var socket = await context.WebSockets.AcceptWebSocketAsync();
            GatewayConnection.Instance.SetSocket(connectionId, socket);

            try
            {
                ValueWebSocketReceiveResult current;
                var message = new List<byte>(); //TODO: ArrayPoolを使いたい

                do
                {
                    Memory<byte> allocatedBuffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);

                    try
                    {
                        current = await socket.ReceiveAsync(allocatedBuffer, cancellationToken);
                        var buffer = allocatedBuffer.Slice(0, current.Count);
                        message.AddRange(buffer.ToArray());

                        if (current.EndOfMessage)
                        {
                            var type = current.MessageType.ToString().ToLower();
                            var uri = $"{_options.ForwardEndpoint}/{type}/{connectionId}";
                            var content = _options.HttpContentGenerator(message.ToArray(), current.MessageType);
                            if (content != null)
                            {
                                #pragma warning disable 4014
                                ForwardMessageAsync(uri, content, cancellationToken).ConfigureAwait(false);
                                #pragma warning restore 4014
                            }
                            else
                            {
                                _logger.LogDebug(GatewayLogEvent.InvalidWebSocketMessageType, $"type: {type}");
                            }

                            message.Clear();
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(allocatedBuffer.ToArray());
                    }

                } while (current.MessageType != WebSocketMessageType.Close);

                async Task ForwardMessageAsync(string requestUri, HttpContent content, CancellationToken ct)
                {
                    var response = await _http.PostAsync(requestUri, content, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogError(GatewayLogEvent.ApplicationEndpointError, $"uri: {requestUri}, status: {response.StatusCode}, body: {body}");
                    }
                }

                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Successfully WebSocket Connection was closed.", cancellationToken);
            }
            finally
            {
                GatewayConnection.Instance.RemoveSocket(connectionId);
            }
        }

        private async Task HandleCallbackRequestAsync(HttpContext context, CancellationToken cancellationToken)
        {
            var connectionId = context.Request.Path.Value.Replace($"{_options.CallbackEndpoint}/", "");

            var socket = GatewayConnection.Instance.GetSocket(connectionId);
            if (socket == null)
            {
                context.Response.StatusCode = 410;
                await context.Response.WriteAsync("Connection was gone.", cancellationToken);
                return;
            }

            var length64 = context.Request.ContentLength ?? context.Request.Body.Length;

            if (length64 > int.MaxValue)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Too large Content-Length.", cancellationToken);
                return;
            }

            var length32 = (int) length64;
            Memory<byte> allocatedBuffer = ArrayPool<byte>.Shared.Rent(length32);
            var buffer = allocatedBuffer.Slice(0, length32);

            try
            {
                await context.Request.Body.ReadAsync(buffer, cancellationToken);
                var type = _options.UnicastMessageTypeSelector(context.Request);

                try
                {
                    // 送信データが指定サイズを超過している場合は分割して複数に分けて送信
                    if (length64 < _options.ReceiveBufferSize)
                    {
                        var chunk = buffer.Slice(0, length32);
                        await socket.SendAsync(chunk, type, true, cancellationToken);
                    }
                    else
                    {
                        var chunkSize = _options.ReceiveBufferSize;
                        var chunkIndex = 0;

                        while (true)
                        {
                            if (chunkIndex + chunkSize <= buffer.Length)
                            {
                                var chunk = buffer.Slice(chunkIndex, chunkSize);
                                chunkIndex += chunkSize;
                                await socket.SendAsync(chunk, type, false, cancellationToken);
                            }
                            else
                            {
                                var finalSize = buffer.Length - chunkIndex;
                                var chunk = buffer.Slice(chunkIndex, finalSize);
                                await socket.SendAsync(chunk, type, true, cancellationToken);
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    GatewayConnection.Instance.RemoveSocket(connectionId);
                    context.Response.StatusCode = 410;
                    await context.Response.WriteAsync($"Connection was gone. id: {connectionId}, exeption: {e}, state: {socket.State}", cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(allocatedBuffer.ToArray());
            }
        }
    }
}