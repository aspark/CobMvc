using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.WebSockets.HttpFake;
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
        /// <summary>
        /// 添加WebSockets支持
        /// </summary>
        /// <param name="web"></param>
        /// <returns></returns>
        public static ICobMvc AddCobWebSockets(this ICobMvc web)
        {
            web.ConfigureServices(services => {
                services.AddSingleton<ICobRequest, CobWebSocketClient>();
                services.AddSingleton<ServerWebSocketPool>(); 
                //services.AddSingleton<ClientWebSocketPool>();
                services.AddSingleton<ClientWebSocketPoolFactory>();
            });

            return web;
        }

        /// <summary>
        /// 启用Websockets
        /// </summary>
        /// <param name="app"></param>
        public static void UseCobWebSockets(this IApplicationBuilder app)
        {
            app.UseCobWebSockets(null);
        }

        /// <summary>
        /// 启用Websockets
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        public static void UseCobWebSockets(this IApplicationBuilder app, WebSocketOptions options)
        {
            //app.ApplicationServices.GetRequiredService<IMvcBuilder>().AddApplicationPart(typeof(WebSocketExtensions).Assembly).AddControllersAsServices();

            if (options == null)
                app.UseWebSockets();
            else
                app.UseWebSockets(options);

            app.Use(async (ctx, next) => {
                if(ctx.WebSockets.IsWebSocketRequest)
                {
                    CobWebSocket2HttpContextBridge.Mount(ctx, next);

                    app.ApplicationServices.GetRequiredService<ServerWebSocketPool>().Enqueue(ctx).Wait();

                    return;
                }

                await next();

            });

        }
    }
}
