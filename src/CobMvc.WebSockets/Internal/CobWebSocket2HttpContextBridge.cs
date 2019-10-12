using CobMvc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CobMvc.WebSockets.HttpFake
{

    /// <summary>
    /// HttpContext执行环境
    /// </summary>
    public class CobWebSocket2HttpContextBridge : ControllerBase
    {
        internal const string EntryUrl = "/cobweb/socket/8FBF718E9D8B41ED8686A604CDF4B833";

        internal static RouteData ConfigRouteData { get; private set; }
        internal static IFeatureCollection Features { get; private set; }

        private static int _hasFake = 0;
        internal static void Mount(HttpContext ctx, Func<Task> next)
        {
            if (Interlocked.CompareExchange(ref _hasFake, 1, 0) == 1)
            {
                MakeUpHttpContext(ctx);
                return;
            }

            ctx.Request.Path = CobWebSocket2HttpContextBridge.EntryUrl;
            ctx.Request.Method = "Get";
            next().ConfigureAwait(false).GetAwaiter().GetResult();//fetch controller route
            ctx.Response.Clear();
        }

        [Route(EntryUrl)]
        public ActionResult Get()
        {
            if (ConfigRouteData == null)
            {
                ConfigRouteData = RouteData;
                Features = Request.HttpContext.Features;
            }

            return NotFound();
        }

        /// <summary>
        /// 补充httpcontext内的上下文
        /// </summary>
        /// <param name="entryContext"></param>
        /// <returns></returns>
        private static HttpContext MakeUpHttpContext(HttpContext entryContext)
        {
            //

            return entryContext;
        }

        internal static async Task<HttpContext> Invoke(HttpContext entryContext, JsonRpcRequest request)
        {
            var route = CobWebSocket2HttpContextBridge.ConfigRouteData.Routers.OfType<IRouteCollection>().FirstOrDefault();
            if (route != null)
            {
                var context = CobWebSocket2HttpContextBridge.CreateHttpContext(entryContext, request);

                var routerContext = new RouteContext(context);
                await route.RouteAsync(routerContext);
                if (routerContext.Handler != null)
                {
                    context.Features.Set<IRoutingFeature>(new RoutingFeature() { RouteData = routerContext.RouteData });
                    await routerContext.Handler.Invoke(context);

                    if (context.Response.Body != null)
                        context.Response.Body.Position = 0;

                    return context;
                }
            }

            return null;
        }

        /// <summary>
        /// 构造httpcontext
        /// </summary>
        /// <param name="entryContext"></param>
        /// <returns></returns>
        internal static HttpContext CreateHttpContext(HttpContext entryContext, JsonRpcRequest request)
        {
            var context = new InMemoryHttpContext(entryContext.Features) { RequestServices = entryContext.RequestServices };
            var uri = new Uri(request.Method, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
            {
                uri = new Uri($"http://localhost/{request.Method.TrimStart('/')}");
            }

            context.Request.Path = uri.AbsolutePath;
            context.Request.QueryString = new QueryString(uri.Query);

            PropertyDescriptorCollection properties = null;
            var isPost = request.Params != null && (properties = TypeDescriptor.GetProperties(request.Params)).Count > 0;
            context.Request.Method = isPost ? "Post" : "Get";
            if (isPost)
            {
                context.Request.Headers.Remove("Content-Type");
                context.Request.Headers.Add("Content-Type", "application/json");

                foreach(var prop in request.Properties)
                {
                    if (prop.Key == CobMvcDefaults.UserAgentValue)
                    {
                        context.Request.Headers["User-Agent"] = prop.Value;

                        continue;
                    }

                    context.Request.Headers[prop.Key] = prop.Value;
                }

                var ms = context.Request.Body = new MemoryStream();
                using (var sw = new StreamWriter(ms, new UTF8Encoding(false), 512, true))
                {
                    if (properties.Count == 1)
                    {
                        sw.Write(JsonConvert.SerializeObject(properties[0].GetValue(request.Params)));//
                    }
                    else
                    {
                        if (properties.Count > 1 && !entryContext.RequestServices.GetRequiredService<IOptions<CobMvcOptions>>().Value.EnableCobMvcParametersBinder)
                        {
                            throw new Exception("find many parameters from body, please set CobMvcOptions.EnableCobMvcParametersBinder");
                        }

                        sw.Write(JsonConvert.SerializeObject(request.Params));//todo:直接Pramas将放入Items，后面由自定义参数绑定赋值参数
                    }
                }

                ms.Position = 0;
            }

            return context;
        }

        private class InMemoryHttpContext : DefaultHttpContext
        {
            private HttpResponse _response = null;

            public InMemoryHttpContext()
            {
                _response = new InMemoryHttpResponse(this);
            }

            public InMemoryHttpContext(IFeatureCollection features) : base(features)
            {
                _response = new InMemoryHttpResponse(this);
            }

            public override HttpResponse Response => _response;

        }

        private class InMemoryHttpResponse : HttpResponse
        {
            public InMemoryHttpResponse(HttpContext context)
            {
                _context = context;
                Body = new MemoryStream();
            }

            private HttpContext _context = null;
            public override HttpContext HttpContext => _context;

            public override int StatusCode { get; set; } = 200;

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
}
