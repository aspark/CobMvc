using CobMvc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{
    public static class WebSocketExtensions
    {
        public static ICobMvc AddCobWebSockets(this ICobMvc web)
        {
            web.ConfigureServices(services => {
                services.AddSingleton<ICobRequest, CobWebSocketClient>();
                services.AddSingleton<ServerWebSocketPool>();
                services.AddSingleton<ClientWebSocketPool>();
            });

            return web;
        }

        public static void UseCobWebSockets(this IApplicationBuilder app)
        {
            app.UseCobWebSockets(null);
        }

        public static void UseCobWebSockets(this IApplicationBuilder app, WebSocketOptions options)
        {
            if (options == null)
                app.UseWebSockets();
            else
                app.UseWebSockets(options);

            app.Use(async (ctx, next) => {
                if(ctx.WebSockets.IsWebSocketRequest)
                {
                    CobWebSocketContextBridge.Mount(ctx, next);

                    app.ApplicationServices.GetRequiredService<ServerWebSocketPool>().Enqueue(ctx).Wait();

                    return;
                }

                await next();

            });

        }
    }

    public class CobWebSocketContextBridge : ControllerBase
    {
        internal const string EntryUrl = "/cobweb/socket/8FBF718E9D8B41ED8686A604CDF4B833";

        public static RouteData ConfigRouteData { get; private set; }
        public static IFeatureCollection Features { get; private set; }

        private static int _hasFake = 0;
        public static void Mount(HttpContext ctx, Func<Task> next)
        {
            if (Interlocked.CompareExchange(ref _hasFake, 1, 0) == 1)
            {
                MakeUpHttpContext(ctx);
            }

            ctx.Request.Path = CobWebSocketContextBridge.EntryUrl;
            ctx.Request.Method = "Get";
            next().Wait();//fetch controller route
            ctx.Response.Clear();
        }

        [Route(EntryUrl)]
        public ActionResult Get()
        {
            if(ConfigRouteData == null)
            {
                ConfigRouteData = RouteData;
                Features = Request.HttpContext.Features;
            }

            return NotFound();
        }

        private static HttpContext MakeUpHttpContext(HttpContext entryContext)
        {
            //add features
            if (entryContext.Features.Get<IRoutingFeature>() == null)
            {
                entryContext.Features.Set(Features.Get<IRoutingFeature>());
            }

            return entryContext;
        }

        public static HttpContext CreateHttpContext(HttpContext entryContext)
        {
            //MakeUpHttpContext(entryContext);

            return new FakeHttpContext(entryContext.Features) { RequestServices = entryContext.RequestServices };
        }

        private class FakeHttpContext : DefaultHttpContext
        {
            private HttpResponse _response = null;

            public FakeHttpContext()
            {
                _response = new FakeHttpResponse(this);
            }

            public FakeHttpContext(IFeatureCollection features) : base(features)
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
}
