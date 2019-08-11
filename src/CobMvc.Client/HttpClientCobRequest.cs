using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Common;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Client
{
    internal class HttpClientCobRequest : CobRequestBase
    {
        HttpClient _client = null;
        CobMvcOptions _options = null;
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<HttpClientCobRequest> _logger = null;

        public HttpClientCobRequest(IOptions<CobMvcOptions> options, IOptions<CobMvcRequestOptions> httpOptions, ICobMvcContextAccessor contextAccessor, ILogger<HttpClientCobRequest> logger)
        {
            _options = options.Value;
            _logger = logger;
            _contextAccessor = contextAccessor;
            _client = new HttpClient();

            if (ServicePointManager.DefaultConnectionLimit < httpOptions.Value.MaxConnetions)//默认将链接数加到100
                ServicePointManager.DefaultConnectionLimit = httpOptions.Value.MaxConnetions;
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
            var usePost = context.Method.GetParameters().Any(p => !p.ParameterType.IsValueTypeOrString());

            return Invoke(context, realType, usePost ? HttpMethod.Post : HttpMethod.Get);
        }

        private async Task<object> Invoke(CobRequestContext context, Type realType, HttpMethod method)
        {
            method = method ?? HttpMethod.Get;

            var passViaBody = (method == HttpMethod.Post || method == HttpMethod.Put) ;

            var url = context.Url;
            var parameters = new Dictionary<string, object>(context.Parameters ?? new Dictionary<string, object>(), StringComparer.InvariantCultureIgnoreCase);
            if (parameters != null && parameters.Any())
            {
                var queries = parameters.Where(p => p.Value != null && p.Value.IsValueTypeOrString()).ToArray();
                if(queries.Length > 0)
                {
                    var query = string.Join("&", queries.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value?.ToString())}"));
                    if (url.Contains('?'))
                        url += "&";
                    else
                        url += "?";

                    url += query;

                    queries.ForEach(p => parameters.Remove(p.Key));
                }
            }

            _logger?.LogDebug("http client begin request:{0}", url);

            var msg = new HttpRequestMessage(method, url);

            if (passViaBody && parameters != null)
            {
                if (parameters.Count == 1)
                {
                    msg.Content = new StringContent(JsonConvert.SerializeObject(parameters.First().Value), Encoding.UTF8, "application/json");
                }
                else
                {
                    if (parameters.Count > 1 && !_options.EnableCobMvcParametersBinder)
                    {
                        throw new Exception("find many parameters from body, please set CobMvcOptions.EnableCobMvcParametersBinder");
                    }

                    msg.Content = new StringContent(JsonConvert.SerializeObject(parameters), Encoding.UTF8, "application/json");//多个class类型的参数，整体推送过去
                }
            }

            //添加traceid等
            msg.Headers.UserAgent.Clear();
            msg.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue(CobMvcDefaults.UserAgentValue, CobMvcDefaults.HeaderUserVersion));
            msg.Headers.Add(CobMvcDefaults.HeaderTraceID, _contextAccessor.Current.TraceID.ToString());
            msg.Headers.Add(CobMvcDefaults.HeaderJump, (_contextAccessor.Current.Jump + 1).ToString());
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
