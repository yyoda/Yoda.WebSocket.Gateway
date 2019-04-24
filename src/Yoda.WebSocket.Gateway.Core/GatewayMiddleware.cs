using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
            _http = httpClientFactory?.CreateClient(GatewayConstant.DefaultHttpClientName) ?? new HttpClient(new SocketsHttpHandler());
            _logger = loggerFactory?.CreateLogger(nameof(GatewayMiddleware)) ?? NullLogger.Instance;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var cancellation = new CancellationTokenSource();

            if (context.Request.Path.StartsWithSegments(_options.WebSocketEndpoint))
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    if (!await _options.AuthenticateHandler(context))
                    {
                        await context.Response.AsUnauthorized(cancellationToken: cancellation.Token);
                        return;
                    }

                    await HandleWebSocketRequestAsync(context, cancellation.Token);
                }
                else
                {
                    await context.Response.AsBadRequest("Invalid WebSocket Protocol", cancellation.Token);
                }
            }
            else if (context.Request.Path.StartsWithSegments(_options.CallbackEndpoint))
            {
                if (!await _options.AuthenticateHandler(context))
                {
                    await context.Response.AsUnauthorized(cancellationToken: cancellation.Token);
                    return;
                }

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
                var message = $"Failed Websocket Handshake. id: {connectionId}";
                await context.Response.AsBadRequest(message, cancellationToken);
                _logger.LogDebug(GatewayConstant.FailedWebSocketHandshake, e, message);
                return;
            }

            WebSocketReference.Instance.SetSocket(connectionId, socket);

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
                            var message = $"Aborted Connection. id: {connectionId}";
                            await context.Response.AsBadRequest(message, cancellationToken);
                            _logger.LogDebug(GatewayConstant.AbortedWebSocketConnection, e, message);
                            return;
                        }

                        var slicedTemporaryMemory = temporaryMemory.Slice(0, current.Count);
                        messageBuffer.AddRange(slicedTemporaryMemory.ToArray());

                        if (current.EndOfMessage)
                        {
                            var content = _options.HttpContentFactory(messageBuffer.ToArray(), current.MessageType);
                            if (content != null)
                            {
                                var requestUri = current.MessageType.ToString().ToLower();
                                var remoteHost = _options.GatewayUrl;
                                var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
                                request.Headers.Add(GatewayConstant.RemoteHostKeyName, remoteHost);
                                request.Headers.Add(GatewayConstant.ConnectionIdKeyName, connectionId);
                                request.Content = content;

                                #pragma warning disable 4014
                                ForwardMessageAsync(request, cancellationToken).ConfigureAwait(false);
                                #pragma warning restore 4014
                            }
                            else
                            {
                                _logger.LogDebug(GatewayConstant.InvalidWebSocketMessageType, $"type: {current.MessageType}");
                            }
                        }
                    }
                    finally
                    {
                        messageBuffer.Clear();
                        ArrayPool<byte>.Shared.Return(temporaryBuffer);
                    }

                } while (current.MessageType != WebSocketMessageType.Close);

                async Task ForwardMessageAsync(HttpRequestMessage request, CancellationToken ct)
                {
                    try
                    {
                        var response = await _http.SendAsync(request, ct);

                        if (!response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            _logger.LogError(GatewayConstant.AbortedHttpRequest, $"uri: {request.RequestUri}, status: {response.StatusCode}, body: {body}");
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(GatewayConstant.AbortedHttpRequest, e, $"uri: {request.RequestUri}");
                    }
                }

                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed WebSocket Connection.", cancellationToken);
                }
                catch (WebSocketException e)
                {
                    var message = $"Failed close WebSocket Connection. id: {connectionId}";
                    await context.Response.AsBadRequest(message, cancellationToken);
                    _logger.LogDebug(GatewayConstant.AbortedWebSocketConnection, e, message);
                }
            }
            finally
            {
                WebSocketReference.Instance.RemoveSocket(connectionId);
            }
        }

        private async Task HandleCallbackRequestAsync(HttpContext context, CancellationToken cancellationToken)
        {
            var connectionId = context.Request.Path.Value.Replace($"{_options.CallbackEndpoint}/", "");
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                await context.Response.AsBadRequest("Missing ConnectionId", cancellationToken);
                return;
            }

            var socket = WebSocketReference.Instance.GetSocket(connectionId);
            if (socket == null)
            {
                await context.Response.AsGone(cancellationToken: cancellationToken);
                return;
            }

            var length64 = context.Request.ContentLength ?? context.Request.Body.Length;
            if (length64 > int.MaxValue)
            {
                await context.Response.AsBadRequest("Too large Content-Length", cancellationToken);
                return;
            }

            var length32 = (int) length64;
            var allocatedBuffer = ArrayPool<byte>.Shared.Rent(length32);
            var readBuffer = allocatedBuffer.AsMemory().Slice(0, length32);

            await context.Request.Body.ReadAsync(readBuffer, cancellationToken);
            var type = _options.WebSocketMessageTypeSelector(context.Request);

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
            catch (WebSocketException e)
            {
                WebSocketReference.Instance.RemoveSocket(connectionId);
                await context.Response.AsGone(cancellationToken: cancellationToken);
                _logger.LogDebug(GatewayConstant.AbortedWebSocketConnection, e, $"WebSocket Connection was Gone. id: {connectionId}, state: {socket.State}");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(allocatedBuffer);
            }
        }
    }
}