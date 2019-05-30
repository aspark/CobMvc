using Cobweb.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb
{
    internal class CobwebMiddleware : IMiddleware
    {
        ICobwebContextFactory _contextFactory = null;
        public CobwebMiddleware(ICobwebContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //添加TraceID等信息
            if(!context.Request.Headers.ContainsKey(CobwebDefaults.HeaderTraceID))
            {
                var traceID = _contextFactory.Current.TraceID;// GetOrAddItem(context, CobwebDefaults.HeaderTraceID, ()=> Guid.NewGuid().ToString());

                context.Request.Headers.Add(CobwebDefaults.HeaderTraceID, traceID.ToString());
            }
            else
            {
                _contextFactory.Current.TraceID = Guid.Parse(context.Request.Headers[CobwebDefaults.HeaderTraceID]);
            }

            return next(context);
        }

        //private string GetOrAddItem(HttpContext context, string key, Func<string> create)
        //{
        //    var value = "";
        //    if (context.Items.ContainsKey(key))
        //    {
        //        value = context.Items[key].ToString();
        //    }
        //    else
        //    {
        //        value = create();
        //        context.Items[key] = value;
        //    }

        //    return value;
        //}
    }
}
