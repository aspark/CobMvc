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
        CobMvcOptions _options = null;
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<CobMvcMiddleware> _logger = null;

        public CobMvcMiddleware(CobMvcOptions options, ICobMvcContextAccessor contextAccessor, ILogger<CobMvcMiddleware> logger)
        {
            _options = options;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            //最长链路限制
            var jump = 0;
            if (context.Request.Headers.ContainsKey(CobMvcDefaults.HeaderJump))
            {
                jump = int.Parse(context.Request.Headers[CobMvcDefaults.HeaderJump]);
            }

            if (jump > _options.MaxJump)
            {
                throw new Exception($"exceed max jump:{_options.MaxJump}");
            }

            _contextAccessor.Current.Jump = jump;

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
