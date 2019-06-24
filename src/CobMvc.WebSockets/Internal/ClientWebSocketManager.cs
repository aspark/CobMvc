using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{

    /// <summary>
    /// 客户端
    /// </summary>
    internal class ClientWebSocketManager : WebSocketWrapper<JsonRpcResponse, JsonRpcRequest>
    {
        private ILoggerFactory _loggerFactory = null;
        private Uri _url = null;
        //private Action<JsonRpcResponse> _callback = null;

        public ClientWebSocketManager(ILoggerFactory loggerFactory, Uri url) : base(loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _url = url;

            //纠正websocket地址
            if (!string.Equals(_url.Scheme, "ws", StringComparison.InvariantCultureIgnoreCase) || !string.Equals(_url.Scheme, "wss", StringComparison.InvariantCultureIgnoreCase))
            {
                var ub = new UriBuilder(_url);
                if (string.Equals(_url.Scheme, "wss", StringComparison.InvariantCultureIgnoreCase))
                    ub.Scheme = "wss";
                else
                    ub.Scheme = "ws";

                _url = ub.Uri;
            }
        }

        public int ID { get; set; }

        public override void Start()
        {
            base.Start();

            Task.Factory.StartNew(KeepAlive, base.Cancellation, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void KeepAlive()
        {
#if !DEBUG
            Task.Delay(30000, base.Cancellation).ContinueWith(t => {
                base.Send(JsonRpcMessages.PingRequest).ConfigureAwait(false).GetAwaiter().GetResult();

                KeepAlive();
            });
#endif
        }

        protected override async Task<WebSocket> GetWebSocket()
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(_url, base.Cancellation);

            return socket;
        }

        ConcurrentDictionary<Guid, TaskCompletionSource<JsonRpcResponse>> _requestList = new ConcurrentDictionary<Guid, TaskCompletionSource<JsonRpcResponse>>();
        public new Task<JsonRpcResponse> Send(JsonRpcRequest request)
        {
            var tcs = new TaskCompletionSource<JsonRpcResponse>();
            _requestList.TryAdd(request.ID, tcs);
            base.Send(request);

            return tcs.Task;
        }

        protected override Task OnReceiveMessage(JsonRpcResponse msg)
        {
            //设置完成
            if (_requestList.TryGetValue(msg.ID, out TaskCompletionSource<JsonRpcResponse> item))
            {
                item.TrySetResult(msg);
                _requestList.TryRemove(msg.ID, out _);//remove completed
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override string ToString()
        {
            return $"send request:{_requestList.Count} {base.ToString()}";
        }
    }

    /// <summary>
    /// 客户端连接池
    /// </summary>
    internal class ClientWebSocketPool : IDisposable
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
            var key = uri.AbsolutePath.GetHashCode() % 10;// pool size: 10 sockets

            _logger.LogDebug($"get or add socket client:{key}");

            ClientWebSocketManager client = null;
            while (true)
            {
                client = _items.GetOrAdd(key, k => {
                    var item = new ClientWebSocketManager(_loggerFactory, uri) { ID = k };
                    item.OnDispose += Item_OnDispose;
                    item.Start();

                    return item;
                });

                if (!client.IsDisposing)
                    break;
            }

            return client;
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
            foreach (var item in _items.Values)
            {
                Dispose(item);
            }
        }
    }
}
