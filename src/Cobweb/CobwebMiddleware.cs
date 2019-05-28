using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb
{
    internal class CobwebMiddleware : IMiddleware
    {
        private const string headerPrefix = "x-cobweb-";
        private const string headerTraceID = headerPrefix + "traceid";

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //添加TraceID等信息
            if(!context.Request.Headers.ContainsKey(headerTraceID))
            {
                var traceID = GetOrAddItem(context, headerTraceID, ()=> Guid.NewGuid());

                context.Request.Headers.Add(headerTraceID, traceID);
            }

            return next(context);
        }

        private string GetOrAddItem(HttpContext context, string key, Func<string> create)
        {
            var value = "";
            if (context.Items.ContainsKey(key))
            {
                value = context.Items[key].ToString();
            }
            else
            {
                value = create();
                context.Items[key] = value;
            }

            return value;
        }
    }
}
