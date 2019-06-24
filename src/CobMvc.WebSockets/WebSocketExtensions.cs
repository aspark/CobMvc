using CobMvc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        private static int _hasFake = 0;
        public static void Mount(HttpContext ctx, Func<Task> next)
        {
            if (Interlocked.CompareExchange(ref _hasFake, 1, 0) == 1)
                return;

            ctx.Request.Path = CobWebSocketContextBridge.EntryUrl;
            ctx.Request.Method = "Get";
            next().Wait();//fetch controller route
            ctx.Response.Clear();
        }

        [Route(EntryUrl)]
        public ActionResult Get()
        {
            if(ConfigRouteData == null)
                ConfigRouteData = RouteData;

            return NotFound();
        }
    }
}
