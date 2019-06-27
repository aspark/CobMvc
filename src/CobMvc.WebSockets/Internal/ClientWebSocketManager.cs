using CobMvc.Core;
using CobMvc.Core.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public override string GetDebugInfo()
        {
            return $"request:{_requestList.Count} {base.ToString()}";
        }
    }

    /// <summary>
    /// 客户端连接池
    /// </summary>
    internal class ClientWebSocketPool : IDisposable
    {
        private ILoggerFactory _loggerFactory = null;
        private ILogger _logger = null;
        private Uri _targetHost = null;
        public ClientWebSocketPool(ILoggerFactory loggerFactory, string targetHost)
        {
            _targetHost = new Uri(targetHost);
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<ClientWebSocketPool>();
        }

        public int MaxPoolSize { get; private set; } = 50;// max pool size
        private volatile int _currentPoolSize = 4;
        private int _poolSizeIncreaseStep = 2;
        private ConcurrentDictionary<int, ClientWebSocketManager> _items = new ConcurrentDictionary<int, ClientWebSocketManager>();
        

        Random _rnd = new Random(Guid.NewGuid().GetHashCode());
        public ClientWebSocketManager Get()
        {
            //增加池大小
            Task.Factory.StartNew(() => {
                if (_currentPoolSize < MaxPoolSize && _items.Count == _currentPoolSize && _items.Values.Where(i => !i.IsDisposing).All(i => i.SendingCount > 0))
                {
                    //increase pool
                    _currentPoolSize = Math.Min(_currentPoolSize + _poolSizeIncreaseStep, MaxPoolSize);
                }
            });

            (int ID, int SendingCount) randomItem = (_rnd.Next() % _currentPoolSize, 0);

            ClientWebSocketManager client = null;
            while (true)
            {
                var key = _items.Values.Select(i => (i.ID, i.SendingCount)).Concat(new[] { randomItem }).OrderBy(v => v.SendingCount).ThenBy(v => _rnd.Next()).First().ID;//.Where(v => !v.IsDisposing)//client.SendingCount最小

                _logger.LogDebug($"get or add socket client:{key}");

                client = _items.GetOrAdd(key, k => {
                    var item = new ClientWebSocketManager(_loggerFactory, _targetHost) { ID = k };
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

        internal string GetDebugInfo()
        {
            return $"pool:{_items.Count} \r\n" + string.Join("\t\r\n", _items.Select(i=>string.Format("{0}->{1}", i.Key, i.Value.GetDebugInfo())));
        }
    }

    internal class ClientWebSocketPoolFactory
    {
        private ILoggerFactory _loggerFactory = null;
        public ClientWebSocketPoolFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        private ConcurrentDictionary<string, ClientWebSocketPool> _items = new ConcurrentDictionary<string, ClientWebSocketPool>();

        //每个服务分配一个池
        public ClientWebSocketPool GetOrCreate(CobRequestContext context)
        {
            return _items.GetOrAdd(context.TargetAddress ?? new Uri(context.Url).AbsolutePath, k => new ClientWebSocketPool(_loggerFactory, context.TargetAddress ?? context.Url));//按serviceName划池
        }

        internal string GetDebugInfo()
        {
            return string.Join("\r\n", _items.Select(i => string.Format("{0}->{1}", i.Key, i.Value.GetDebugInfo())));
        }
    }
}
