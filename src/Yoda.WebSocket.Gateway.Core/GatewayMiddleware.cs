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
        private readonly HttpClient _http;
        private readonly ILogger _logger;

        public GatewayMiddleware(RequestDelegate next, GatewayOptions options, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _http = httpClientFactory.CreateClient("default");
            _logger = loggerFactory.CreateLogger(nameof(GatewayMiddleware));
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
            System.Net.WebSockets.WebSocket socket;

            try
            {
                socket = await context.WebSockets.AcceptWebSocketAsync();
            }
            catch (Exception e)
            {
                const string errorMessage = "Websocket handshake error.";
                _logger.LogDebug(GatewayLogEvent.WebSocketHandshakeError, e, errorMessage);
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync(errorMessage, cancellationToken);
                return;
            }

            GatewayConnection.Instance.SetSocket(connectionId, socket);

            try
            {
                ValueWebSocketReceiveResult current;
                var messageBuffer = new List<byte>();

                do
                {
                    var temporaryBuffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);
                    Memory<byte> temporaryMemory = temporaryBuffer;

                    try
                    {
                        try
                        {
                            current = await socket.ReceiveAsync(temporaryMemory, cancellationToken);
                        }
                        catch (WebSocketException e)
                        {
                            const string errorMessage = "Connection has been disabled.";
                            _logger.LogDebug(GatewayLogEvent.WebSocketConnectionError, e, errorMessage);
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync(errorMessage, cancellationToken);
                            return;
                        }

                        var slicedBuffer = temporaryMemory.Slice(0, current.Count);
                        messageBuffer.AddRange(slicedBuffer.ToArray());

                        if (current.EndOfMessage)
                        {
                            var type = current.MessageType.ToString().ToLower();
                            var content = _options.HttpContentGenerator(messageBuffer.ToArray(), current.MessageType);
                            if (content != null)
                            {
                                #pragma warning disable 4014
                                ForwardMessageAsync($"{type}/{connectionId}", content, cancellationToken).ConfigureAwait(false);
                                #pragma warning restore 4014
                            }
                            else
                            {
                                _logger.LogDebug(GatewayLogEvent.InvalidWebSocketMessageType, $"type: {type}");
                            }
                        }
                    }
                    finally
                    {
                        messageBuffer.Clear();
                        ArrayPool<byte>.Shared.Return(temporaryBuffer);
                    }

                } while (current.MessageType != WebSocketMessageType.Close);

                async Task ForwardMessageAsync(string requestUri, HttpContent content, CancellationToken ct)
                {
                    var response = await _http.PostAsync(requestUri, content, ct);

                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        _logger.LogError(GatewayLogEvent.HttpRequestError, $"uri: {requestUri}, status: {response.StatusCode}, body: {body}");
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
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("ConnectionId does not exist.", cancellationToken);
                return;
            }

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
            var allocatedBuffer = ArrayPool<byte>.Shared.Rent(length32);
            var readBuffer = allocatedBuffer.AsMemory().Slice(0, length32);

            try
            {
                await context.Request.Body.ReadAsync(readBuffer, cancellationToken);
                var type = _options.UnicastMessageTypeSelector(context.Request);

                try
                {
                    // 送信データが指定サイズを超過している場合は分割して複数に分けて送信
                    if (length64 < _options.ReceiveBufferSize)
                    {
                        var chunk = readBuffer.Slice(0, length32);
                        await socket.SendAsync(chunk, type, true, cancellationToken);
                    }
                    else
                    {
                        var chunkSize = _options.ReceiveBufferSize;
                        var chunkIndex = 0;

                        while (true)
                        {
                            if (chunkIndex + chunkSize <= readBuffer.Length)
                            {
                                var chunk = readBuffer.Slice(chunkIndex, chunkSize);
                                chunkIndex += chunkSize;
                                await socket.SendAsync(chunk, type, false, cancellationToken);
                            }
                            else
                            {
                                var finalSize = readBuffer.Length - chunkIndex;
                                var chunk = readBuffer.Slice(chunkIndex, finalSize);
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
                ArrayPool<byte>.Shared.Return(allocatedBuffer);
            }
        }
    }
}