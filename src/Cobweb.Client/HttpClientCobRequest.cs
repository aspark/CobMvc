using Cobweb.Core;
using Cobweb.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Client
{
    public class HttpClientCobRequest : ICobRequest
    {
        HttpClient _client = null;
        ICobwebContextFactory _contextFactory = null;
        ILogger<HttpClientCobRequest> _logger = null;

        public HttpClientCobRequest(ICobwebContextFactory contextFactory, ILogger<HttpClientCobRequest> logger)
        {
            _logger = logger;
            _contextFactory = contextFactory;
            _client = new HttpClient();
        }

        //支持HttpMethod, 
        //todo:HttpPost FromBodyAttribute需要添加mvc core 引用？
        public Task<object> DoRequest(CobRequestContext context, object state)
        {
            if (context is TypedCobRequestContext)
                return Invoke(context as TypedCobRequestContext);

            if(state != null && !(state is HttpClientCobRequestOptions))
            {
                throw new ArgumentException("state should be HttpClientCobRequestOptions");
            }

            return Invoke(context, state as HttpClientCobRequestOptions);
        }

        private Task<object> Invoke(CobRequestContext context, HttpClientCobRequestOptions options)
        {
            options = options ?? new HttpClientCobRequestOptions();

            return Invoke(context, options.Method);
        }

        private Task<object> Invoke(TypedCobRequestContext context)
        {
            var usePost = context.Method.GetParameters().Any(p => p.ParameterType.IsClass && p.ParameterType != typeof(string));

            return Invoke(context, usePost ? HttpMethod.Post : HttpMethod.Get);
        }

        private async Task<object> Invoke(CobRequestContext context, HttpMethod method)
        {
            method = method ?? HttpMethod.Get;

            var passViaBody = (method == HttpMethod.Post || method == HttpMethod.Put) ;

            var url = context.Url;
            if (!passViaBody)
            {
                if (context.Parameters != null && context.Parameters.Any())
                {
                    var query = string.Join("&", context.Parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString())}"));
                    if (url.Contains('?'))
                        url += "&";
                    else
                        url += "?";

                    url += query;
                }
            }

            _logger?.LogDebug("http client begin request:{0}", url);

            var msg = new HttpRequestMessage(method, url);

            if (passViaBody)
            {
                msg.Content = new StringContent(JsonConvert.SerializeObject(context.Parameters), Encoding.UTF8, "application/json");
            }

            //添加traceid等
            msg.Headers.UserAgent.Clear();
            msg.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(CobwebDefaults.UserAgent, "0.0.1"));
            msg.Headers.Add(CobwebDefaults.HeaderTraceID, _contextFactory.Current.TraceID.ToString());
            _logger?.LogDebug("set http request traceID:{0}", _contextFactory.Current.TraceID);

            var response = await _client.SendAsync(msg);

            response.EnsureSuccessStatusCode();//抛出异常

            if(context.ReturnType != null)
            {
                return JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), context.ReturnType);
            }

            return null;
        }
    }

    public class HttpClientCobRequestOptions
    {
        public HttpClientCobRequestOptions()
        {
            Method = HttpMethod.Get;
        }

        public HttpMethod Method { get; set; }
    }
}
