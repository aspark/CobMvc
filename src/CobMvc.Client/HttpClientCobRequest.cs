using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Client
{
    internal class HttpClientCobRequest : CobRequestBase
    {
        HttpClient _client = null;
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<HttpClientCobRequest> _logger = null;

        public HttpClientCobRequest(ICobMvcContextAccessor contextAccessor, ILogger<HttpClientCobRequest> logger)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _client = new HttpClient();
        }

        public override string SupportTransport { get => CobRequestTransports.Http; }

        //支持HttpMethod, 
        //todo:HttpPost FromBodyAttribute需要添加mvc core 引用？
        protected override Task<object> DoRequest(CobRequestContext context, Type realType, object state)
        {
            if (context is TypedCobRequestContext)
                return Invoke(context as TypedCobRequestContext, realType);

            if(state != null && !(state is HttpClientCobRequestOptions))
            {
                throw new ArgumentException("state should be HttpClientCobRequestOptions");
            }

            return Invoke(context, realType, state as HttpClientCobRequestOptions);
        }

        private Task<object> Invoke(CobRequestContext context, Type realType, HttpClientCobRequestOptions options)
        {
            options = options ?? new HttpClientCobRequestOptions();

            return Invoke(context, realType, options.Method);
        }

        private Task<object> Invoke(TypedCobRequestContext context, Type realType)
        {
            var usePost = context.Method.GetParameters().Any(p => p.ParameterType.IsClass && p.ParameterType != typeof(string));

            return Invoke(context, realType, usePost ? HttpMethod.Post : HttpMethod.Get);
        }

        private async Task<object> Invoke(CobRequestContext context, Type realType, HttpMethod method)
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
            msg.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(CobMvcDefaults.UserAgent, "0.0.1"));
            msg.Headers.Add(CobMvcDefaults.HeaderTraceID, _contextAccessor.Current.TraceID.ToString());
            _logger?.LogDebug("set http request traceID:{0}", _contextAccessor.Current.TraceID);

            var response = await _client.SendAsync(msg);

            response.EnsureSuccessStatusCode();//抛出异常

            var content = await response.Content.ReadAsStringAsync();

            var value = realType != null ? JsonConvert.DeserializeObject(content, realType) : null;

            return value;
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
