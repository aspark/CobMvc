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
        ICobwebContextFactory _contextFactory = null;
        ILogger<CobwebMiddleware> _logger = null;

        public CobwebMiddleware(ICobwebContextFactory contextFactory, ILogger<CobwebMiddleware> logger)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //添加TraceID等信息
            Guid traceID;
            if (!context.Request.Headers.ContainsKey(CobwebDefaults.HeaderTraceID))
            {
                traceID = _contextFactory.Current.TraceID;// GetOrAddItem(context, CobwebDefaults.HeaderTraceID, ()=> Guid.NewGuid().ToString());

                context.Request.Headers.Add(CobwebDefaults.HeaderTraceID, traceID.ToString());

                _logger.LogInformation("mark request. traceID:{0}", traceID);
            }
            else
            {
                traceID = Guid.Parse(context.Request.Headers[CobwebDefaults.HeaderTraceID]);
                _contextFactory.Current.TraceID = traceID;
                _logger.LogInformation("receive request. traceID:{0}", traceID);
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
