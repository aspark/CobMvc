using CobMvc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc
{
    internal class CobMvcMiddleware : IMiddleware
    {
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<CobMvcMiddleware> _logger = null;

        public CobMvcMiddleware(ICobMvcContextAccessor contextAccessor, ILogger<CobMvcMiddleware> logger)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            //todo:最长链路限制

            //为所有进入的请求添加TraceID等信息
            Guid traceID;
            if (!context.Request.Headers.ContainsKey(CobMvcDefaults.HeaderTraceID))
            {
                traceID = _contextAccessor.Current.TraceID;// GetOrAddItem(context, CobMvcDefaults.HeaderTraceID, ()=> Guid.NewGuid().ToString());

                context.Request.Headers.Add(CobMvcDefaults.HeaderTraceID, traceID.ToString());

                _logger.LogDebug("mark request. traceID:{0}", traceID);
            }
            else
            {
                traceID = Guid.Parse(context.Request.Headers[CobMvcDefaults.HeaderTraceID]);
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
