using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{
    /// <summary>
    /// 管理Websocket，接收或发送消息
    /// </summary>
    /// <typeparam name="TRec"></typeparam>
    /// <typeparam name="TSend"></typeparam>
    internal abstract class WebSocketWrapper<TRec, TSend> : IDisposable where TRec : JsonRpcBase where TSend: JsonRpcBase
    {
        private ILogger<WebSocketWrapper<TRec, TSend>> _logger = null;
        private CancellationTokenSource _cts = null;

        public WebSocketWrapper(ILoggerFactory loggerFactory)
        {
            _cts = new CancellationTokenSource();
            _logger = loggerFactory.CreateLogger<WebSocketWrapper<TRec, TSend>>();
        }

        private Task _waitHandle = null;
        protected virtual void Init()
        {
            var socket = HandleSocket();

            var messages = Task.Factory.StartNew(HandleMessages, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            var send = Task.Factory.StartNew(SendResponse, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _waitHandle = socket.ContinueWith(t=>Dispose());
        }

        protected abstract Task<WebSocket> GetWebSocket();

        public void Wait()
        {
            _waitHandle.Wait();
        }

        protected CancellationToken Cancellation { get => _cts.Token; }

        private WebSocket _websocket = null;
        private async Task HandleSocket()
        {
            _websocket = await GetWebSocket();

            //change to Rx?
            var pipe = new Pipe();
            var write = FillPipe(_websocket, pipe.Writer);
            var read = ReadPipe(_websocket, pipe.Reader);

            await Task.WhenAll(write, read);
        }

        private const int _minBufferSize = 512;

        private async Task FillPipe(WebSocket socket, PipeWriter writer)
        {
            while(socket.State < WebSocketState.Closed)
            {
                var memory = writer.GetMemory(_minBufferSize + 2);

                try
                {
                    var buffer = new byte[_minBufferSize];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.Count > 0)
                    {
                        buffer.CopyTo(memory);
                    }

                    var count = result.Count;

                    if(result.EndOfMessage)
                    {
                        var split = memory.Slice(count);
                        split.Span[0] = 30;
                        split.Span[1] = 31;
                        count += 2;
                    }

                    writer.Advance(count);
                }
                catch (WebSocketException ex)
                {
                    _logger.LogError(ex, "consume websocket failed");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "consume websocket failed");
                }

                if ((await writer.FlushAsync()).IsCompleted)
                    break;
            }

            writer.Complete();
        }

        ////cob[00][ver]\n[4-length]\n[data]\n
        ////cob_00_01\n00_00_00_0F\n[8bytes]
        ////ver 1:jsonp
        private async Task ReadPipe(WebSocket socket, PipeReader reader)
        {
            while (socket.State < WebSocketState.Closed)
            {
                ReadResult result = await reader.ReadAsync();
                try
                {

                    ReadOnlySequence<byte> buffer = result.Buffer;
                    SequencePosition? position = null;

                    do
                    {
                        position = LastPositionOf(buffer, (byte)30);

                        if (position != null)
                        {
                            var spliter = buffer.Slice(position.Value);
                            if (spliter.Length >= 2 && spliter.First.Span[1] == 31)
                            {
                                var block = buffer.Slice(0, position.Value);

                                ReceiveMessage(block);

                                buffer = buffer.Slice(buffer.GetPosition(2, position.Value));
                            }
                        }
                        else
                            break;
                    }
                    while (buffer.Length > 0);

                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
                catch (Exception ex)
                {

                }

                if (result.IsCompleted)
                {
                    break;
                }
            }

            reader.Complete();
        }

        private SequencePosition? LastPositionOf(ReadOnlySequence<byte> buffer, byte find)
        {
            ////var index = buffer.Length;
            //foreach(var item in buffer)
            //{
            //    for (var i = item.Span.Length - 1; i >= 0; i--)
            //    {
            //        if (item.Span[i] == find)
            //            return new SequencePosition(item, i);
            //    }
            //}

            //return null;

            return buffer.PositionOf(find);
        }

        private BlockingCollection<TRec> _messages = new BlockingCollection<TRec>();
        private void ReceiveMessage(ReadOnlySequence<byte> buffer)
        {
            var body = Encoding.UTF8.GetString(buffer.ToArray());

            Console.WriteLine(body);

            try
            {
                var rpc = JsonConvert.DeserializeObject<TRec>(body);
                _messages.Add(rpc);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"parse to '{typeof(TRec).Name}' failed");
            }

        }

        private async Task HandleMessages()
        {
            while (!_messages.IsCompleted && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (_messages.TryTake(out TRec request))
                    {
                        await OnReceiveMessage(request);
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "HandleMessages");
                }
            }
        }

        protected abstract Task OnReceiveMessage(TRec msg);

        public event EventHandler OnDispose;

        private bool _isDisposing = false;
        public void Dispose()
        {
            if (_isDisposing)
                return;

            lock (this)
            {
                if (_isDisposing)
                    return;

                _isDisposing = true;
            }

            _logger.LogDebug("websocket disposed");

            OnDispose?.Invoke(this, EventArgs.Empty);

            _cts.Cancel();
            _messages.Dispose();
            _sendList.Dispose();
            _websocket?.Dispose();
        }

        BlockingCollection<(TaskCompletionSource<bool> Source, TSend Content)> _sendList = new BlockingCollection<(TaskCompletionSource<bool>, TSend)>();

        /// <summary>
        /// 发送回复
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public virtual Task<bool> Send(TSend response)
        {
            if (response == null)
                return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            _sendList.Add((tcs, response));

            return tcs.Task;
        }

        public virtual void SendAndForget(TSend response)
        {
            Send(response).ConfigureAwait(false);
        }

        private void SendResponse()
        {
            while (!_sendList.IsCompleted && !_cts.IsCancellationRequested)
            {
                try
                {
                    if (_sendList.TryTake(out (TaskCompletionSource<bool> Source, TSend Content) pair))
                    {
                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pair.Content));
                        for (var i = 0; i < bytes.Length; i += _minBufferSize)
                        {
                            var length = Math.Min(bytes.Length - i, _minBufferSize);

                            _websocket.SendAsync(new ArraySegment<byte>(bytes, i, length), WebSocketMessageType.Text, length < _minBufferSize, _cts.Token).ConfigureAwait(false).GetAwaiter().GetResult();
                        }

                        pair.Source.TrySetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SendResponse");
                }
            }
        }
    }

    /// <summary>
    /// 服务端
    /// </summary>
    internal class ServerWebSocketManager : WebSocketWrapper<JsonRpcRequest, JsonRpcResponse>
    {
        private ILoggerFactory _loggerFactory = null;
        private HttpContext _context = null;
        //private Action<JsonRpcRequest> callback = null;

        public ServerWebSocketManager(ILoggerFactory loggerFactory, HttpContext context) : base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _context = context;
            Init();
        }

        protected override Task<WebSocket> GetWebSocket()
        {
            return _context.WebSockets.AcceptWebSocketAsync();
        }

        protected override async Task OnReceiveMessage(JsonRpcRequest msg)
        {
            if (msg != null)
            {
                if (string.Equals(msg.Method, JsonRpcMessages.PingRequest.Method, StringComparison.InvariantCultureIgnoreCase))
                {
                    await base.Send(JsonRpcMessages.PongResponse);
                    return;
                }

                //todo:invoke mvc handle

                var route = CobWebSocketContextBag.ConfigRouteData.Routers.OfType<IRouteCollection>().First();

                //var context = _context.RequestServices.GetRequiredService<IHttpContextFactory>().Create(_context.Features);
                var context = new DefaultHttpContext(_context.Features) { RequestServices = _context.RequestServices };
                context.Request.Path = msg.Method;
                context.Request.Method = "Get";


                var routerContext = new RouteContext(context);
                await route.RouteAsync(routerContext);
                if (routerContext.Handler != null)
                {
                    await routerContext.Handler.Invoke(context);

                    string body = "";
                    using (var sr = new StreamReader(context.Response.Body))
                    {
                        body = await sr.ReadToEndAsync();
                    }

                    if (context.Response.StatusCode == 200)
                    {
                        var res = new JsonRpcResponse() { ID = msg.ID, Result = JsonConvert.DeserializeObject(body) };
                        foreach(var header in context.Response.Headers)
                        {
                            res.Properties[header.Key] = header.Value;
                        }

                        base.SendAndForget(res);//todo:编解码了多次

                        return;
                    }
                    else
                    {
                        var error = JsonRpcMessages.CreateError(msg.ID, context.Response.StatusCode, ((HttpStatusCode)context.Response.StatusCode).ToString());
                        error.Error.Data = body;
                        base.SendAndForget(error);

                        return;
                    }
                }

                base.SendAndForget(JsonRpcMessages.CreateError(msg.ID, "can not route to action"));

                return;
            }

            base.SendAndForget(JsonRpcMessages.CreateError("msg is empty"));
        }
    }

    /// <summary>
    /// 服务端链接管理
    /// </summary>
    internal class ServerWebSocketPool : IDisposable
    {
        private ILoggerFactory _loggerFactory = null;
        private ILogger _logger = null;
        public ServerWebSocketPool(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ServerWebSocketPool>();
        }

        private ConcurrentDictionary<ServerWebSocketManager, bool> _items = new ConcurrentDictionary<ServerWebSocketManager, bool>();
        public ServerWebSocketManager Enqueue(HttpContext context)
        {
            _logger.LogDebug($"receive websocket:{context.Request.GetDisplayUrl()}");

            var item = new ServerWebSocketManager(_loggerFactory, context);
            item.OnDispose += Item_OnDispose;
            _items.TryAdd(item, true);

            return item;
        }

        private void Item_OnDispose(object sender, EventArgs e)
        {
            Dispose(sender as ServerWebSocketManager);
        }

        public void Dispose(ServerWebSocketManager item)
        {
            _items.TryRemove(item, out _);
            item.OnDispose -= Item_OnDispose;
            item.Dispose();
        }

        public void Dispose()
        {
            foreach(var item in _items.Keys)
            {
                Dispose(item);
            }
        }
    }

    /// <summary>
    /// 客户端
    /// </summary>
    internal class ClientWebSocketManager: WebSocketWrapper<JsonRpcResponse, JsonRpcRequest>
    {
        private ILoggerFactory _loggerFactory = null;
        private Uri _url = null;
        //private Action<JsonRpcResponse> _callback = null;

        public ClientWebSocketManager(ILoggerFactory loggerFactory, Uri url) : base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _url = url;
            Init();
        }

        public int ID { get; set; }

        protected override void Init()
        {
            base.Init();

            Task.Factory.StartNew(KeepAlive, base.Cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void KeepAlive()
        {
            Task.Delay(10000, base.Cancellation).ContinueWith(t => {
                base.Send(JsonRpcMessages.PingRequest).ConfigureAwait(false).GetAwaiter().GetResult();

                KeepAlive();
            });
        }

        protected override async Task<WebSocket> GetWebSocket()
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(_url, base.Cancellation);

            return socket;
        }

        ConcurrentDictionary<Guid, TaskCompletionSource<JsonRpcResponse>> _sendList = new ConcurrentDictionary<Guid, TaskCompletionSource<JsonRpcResponse>>();
        public new Task<JsonRpcResponse> Send(JsonRpcRequest response)
        {
            var tcs = new TaskCompletionSource<JsonRpcResponse>();
            _sendList.TryAdd(response.ID, tcs);
            base.Send(response);

            return tcs.Task;
        }

        protected override Task OnReceiveMessage(JsonRpcResponse msg)
        {
            //设置完成
            if(_sendList.TryGetValue(msg.ID, out TaskCompletionSource<JsonRpcResponse> item))
            {
                item.TrySetResult(msg);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 客户端连接池
    /// </summary>
    internal class ClientWebSocketPool: IDisposable
    {
        private ILoggerFactory _loggerFactory = null;
        private ILogger _logger = null;
        public ClientWebSocketPool(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ClientWebSocketPool>();
        }

        private ConcurrentDictionary<int, ClientWebSocketManager> _items = new ConcurrentDictionary<int, ClientWebSocketManager>();
        public ClientWebSocketManager GetOrCreate(string url)
        {
            var uri = new Uri(url);
            var key = uri.AbsolutePath.GetHashCode()%10;// pool size: 10 sockets

            _logger.LogDebug($"get or add socket client:{key}");

            return _items.GetOrAdd(key, k => {
                var item = new ClientWebSocketManager(_loggerFactory, uri) { ID = k };
                item.OnDispose += Item_OnDispose;

                return item;
            });
        }

        private void Item_OnDispose(object sender, EventArgs e)
        {
            Dispose(sender as ClientWebSocketManager);
        }

        public void Dispose(ClientWebSocketManager item)
        {
            _items.TryRemove(item.ID, out _);
            item.OnDispose -= Item_OnDispose;
            item.Dispose();
        }

        public void Dispose()
        {
            foreach(var item in _items.Values)
            {
                Dispose(item);
            }
        }
    }
}
