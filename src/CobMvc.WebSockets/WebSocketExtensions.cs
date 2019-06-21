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
                    CobWebSocketContextBag.FakeController(ctx, next);

                    app.ApplicationServices.GetRequiredService<ServerWebSocketPool>().Enqueue(ctx).Wait();

                    return;
                }

                await next();

            });

        }
    }

    public class CobWebSocketContextBag : ControllerBase
    {
        internal const string EntryUrl = "/cobweb/8FBF718E9D8B41ED8686A604CDF4B833.socket";

        public static RouteData ConfigRouteData { get; private set; }

        private static object _objHasFake = new object();
        private static bool _hasFake = false;
        public static void FakeController(HttpContext ctx, Func<Task> next)
        {
            if (_hasFake)
                return;
            
            lock (_objHasFake)
            {
                if (_hasFake)
                    return;

                ctx.Request.Path = CobWebSocketContextBag.EntryUrl;
                ctx.Request.Method = "Get";
                next().Wait();//fetch controller route
                ctx.Response.Clear();

                _hasFake = true;
            }
        }

        [Route(EntryUrl)]
        public void Get()
        {
            if(ConfigRouteData == null)
                ConfigRouteData = RouteData;
        }
    }
}
