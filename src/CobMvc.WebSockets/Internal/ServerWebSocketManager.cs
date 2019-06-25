using CobMvc.WebSockets.HttpFake;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
                    await base.Send(JsonRpcMessages.GetPongResponse(msg.ID));
                    return;
                }

                //todo:invoke mvc handle

                try
                {
                    var route = CobWebSocket2HttpContextBridge.ConfigRouteData.Routers.OfType<IRouteCollection>().FirstOrDefault();
                    if (route != null)
                    {
                        var context = CobWebSocket2HttpContextBridge.CreateHttpContext(_context);
                        var uri = new Uri(msg.Method, UriKind.RelativeOrAbsolute);
                        if(!uri.IsAbsoluteUri)
                        {
                            uri = new Uri($"http://localhost/{msg.Method.TrimStart('/')}");
                        }

                        context.Request.Path = uri.AbsolutePath;
                        context.Request.QueryString = new QueryString(uri.Query);
                        
                        var isPost = msg.Params != null && TypeDescriptor.GetProperties(msg.Params).Count > 0;
                        context.Request.Method = isPost ? "Post" : "Get";
                        if(isPost)
                        {
                            var ms = context.Request.Body = new MemoryStream();
                            using (var sw = new StreamWriter(ms, Encoding.UTF8, 512, true))
                            {
                                sw.Write(JsonConvert.SerializeObject(msg.Params));
                                ms.Position = 0;
                            }
                        }

                        var routerContext = new RouteContext(context);
                        await route.RouteAsync(routerContext);
                        if (routerContext.Handler != null)
                        {
                            context.Features.Set<IRoutingFeature>(new RoutingFeature() { RouteData = routerContext.RouteData });
                            await routerContext.Handler.Invoke(context);

                            string body = "";
                            using (var ms = context.Response.Body)
                            {
                                ms.Position = 0;//???
                                using (var sr = new StreamReader(ms))
                                {
                                    body = await sr.ReadToEndAsync();
                                }
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
            item.Start();
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
