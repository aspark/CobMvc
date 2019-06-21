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
            if (_sendList.TryGetValue(msg.ID, out TaskCompletionSource<JsonRpcResponse> item))
            {
                item.TrySetResult(msg);
            }

            return Task.CompletedTask;
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
            foreach (var item in _items.Values)
            {
                Dispose(item);
            }
        }
    }
}
