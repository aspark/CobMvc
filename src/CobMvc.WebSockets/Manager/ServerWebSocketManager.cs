using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{

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

                try
                {
                    var route = CobWebSocketContextBag.ConfigRouteData.Routers.OfType<IRouteCollection>().First();

                    //var context = _context.RequestServices.GetRequiredService<IHttpContextFactory>().Create(_context.Features);
                    var context = new FakeHttpContext(_context.Features) { RequestServices = _context.RequestServices };
                    context.Request.Path = msg.Method;
                    context.Request.Method = "Get";


                    var routerContext = new RouteContext(context);
                    await route.RouteAsync(routerContext);
                    if (routerContext.Handler != null)
                    {
                        await routerContext.Handler.Invoke(context);

                        string body = "";
                        context.Response.Body.Position = 0;//???
                        using (var sr = new StreamReader(context.Response.Body))
                        {
                            body = await sr.ReadToEndAsync();
                        }

                        if (context.Response.StatusCode == 0)//todo:200
                        {
                            var res = new JsonRpcResponse() { ID = msg.ID, Result = JsonConvert.DeserializeObject(body) };
                            foreach (var header in context.Response.Headers)
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
                catch(Exception ex)
                {
                    base.SendAndForget(JsonRpcMessages.CreateError(msg.ID, ex.Message));

                    throw ex;
                }
            }

            base.SendAndForget(JsonRpcMessages.CreateError("msg is empty"));
        }

        private class FakeHttpContext : DefaultHttpContext
        {
            private HttpResponse _response = null;

            public FakeHttpContext(IFeatureCollection features) :base(features)
            {
                _response = new FakeHttpResponse(this);
            }

            public override HttpResponse Response => _response;
        }

        private class FakeHttpResponse : HttpResponse
        {
            public FakeHttpResponse(HttpContext context)
            {
                _context = context;
                Body = new MemoryStream();
            }

            private HttpContext _context = null;
            public override HttpContext HttpContext => _context;

            public override int StatusCode { get; set; }

            private HeaderDictionary _header = new HeaderDictionary();
            public override IHeaderDictionary Headers => _header;

            public override Stream Body { get; set; }
            public override long? ContentLength { get; set; }
            public override string ContentType { get; set; }

            public override IResponseCookies Cookies => throw new NotImplementedException();

            public override bool HasStarted => false;

            public override void OnCompleted(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override async void OnStarting(Func<object, Task> callback, object state)
            {
                await callback(state);
            }

            public override void Redirect(string location, bool permanent)
            {
                throw new NotImplementedException();
            }
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
            foreach (var item in _items.Keys)
            {
                Dispose(item);
            }
        }
    }
}
