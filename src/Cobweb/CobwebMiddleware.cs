using Cobweb.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb
{
    internal class CobwebMiddleware : IMiddleware
    {
        ICobwebContextAccessor _contextAccessor = null;
        ILogger<CobwebMiddleware> _logger = null;

        public CobwebMiddleware(ICobwebContextAccessor contextAccessor, ILogger<CobwebMiddleware> logger)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            //todo:最长链路限制

            //为所有进入的请求添加TraceID等信息
            Guid traceID;
            if (!context.Request.Headers.ContainsKey(CobwebDefaults.HeaderTraceID))
            {
                traceID = _contextAccessor.Current.TraceID;// GetOrAddItem(context, CobwebDefaults.HeaderTraceID, ()=> Guid.NewGuid().ToString());

                context.Request.Headers.Add(CobwebDefaults.HeaderTraceID, traceID.ToString());

                _logger.LogDebug("mark request. traceID:{0}", traceID);
            }
            else
            {
                traceID = Guid.Parse(context.Request.Headers[CobwebDefaults.HeaderTraceID]);
                _contextAccessor.Current.TraceID = traceID;
                _logger.LogDebug("receive request. traceID:{0}", traceID);
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
