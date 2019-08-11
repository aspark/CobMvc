using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using CobMvc.Core;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using CobMvc.Core.Common;

namespace CobMvc
{
    internal class CobMvcParametersBinder : IActionFilter//IResourceFilter
    {
        CobMvcOptions _options = null;
         ICobMvcContextAccessor _contextAccessor = null;
        ILogger<CobMvcContextMiddleware> _logger = null;

        public CobMvcParametersBinder(IOptions<CobMvcOptions> options, ICobMvcContextAccessor contextAccessor, ILogger<CobMvcContextMiddleware> logger)
        {
            _options = options.Value;
            _logger = logger;
            _contextAccessor = contextAccessor;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (_options.EnableCobMvcParametersBinder && context.HttpContext.Request.Headers[CobMvcDefaults.HeaderUserAgent].ToString().Contains(CobMvcDefaults.UserAgentValue))//只对cobmvc类型的请求做处理
            {
                if (context.ActionDescriptor.Parameters.Count(p => !p.ParameterType.IsValueTypeOrString()) <= 1)//EnableCobMvcParametersBinder暂只处理多个class类型的参数
                    return;

                context.HttpContext.Request.EnableBuffering();
                if (context.HttpContext.Request.Body.Length > 0)
                {
                    var parameters = context.ActionDescriptor.Parameters.ToDictionary(p => p.Name);

                    context.HttpContext.Request.Body.Position = 0;

                    using (var sr = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, true, 512, true))//todo:从Items中获取字典
                    {
                        var obj = JToken.Parse(sr.ReadToEnd());
                        if(obj.Type == JTokenType.Object)
                        {
                            foreach(var prop in (obj as JObject).Properties())
                            {
                                //if (!context.ActionArguments.ContainsKey(prop.Name))
                                //{
                                //    context.ActionArguments[prop.Name] = prop.Value;
                                //}
                                context.ActionArguments[prop.Name] = (prop.Value as JToken).ToObject(parameters[prop.Name].ParameterType);
                            }
                        }
                    }

                    context.HttpContext.Request.Body.Position = 0;
                }
            }

        }
    }

    //internal class CobMvcParametersProvider : IValueProvider
    //{
    //    public bool ContainsPrefix(string prefix)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ValueProviderResult GetValue(string key)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //internal class CobMvcParametersProviderFactory : IValueProviderFactory
    //{
    //    public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    //    {
    //        //context.ActionContext.HttpContext
    //    }
    //}
}
