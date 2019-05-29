using Cobweb.Core;
using Cobweb.Core.Service;
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
        public HttpClientCobRequest()
        {
            _client = new HttpClient();
        }

        //todo:支持HttpMethod
        public Task<object> DoRequest(CobRequestContext context, object[] states)
        {
            return Invoke(context);
        }

        public async Task<object> Invoke(CobRequestContext context)
        {
            var usePost = context.Method.GetParameters().Any(p => p.ParameterType.IsClass && p.ParameterType != typeof(string));

            var url = context.Url;
            if(!usePost)
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

            var msg = new HttpRequestMessage(usePost ? HttpMethod.Post : HttpMethod.Get, url);

            if(usePost)
            {
                msg.Content = new StringContent(JsonConvert.SerializeObject(context.Parameters), Encoding.UTF8, "application/json");
            }

            //todo:添加traceid等
            //msg.Headers.Add()
            var response = await _client.SendAsync(msg);

            response.EnsureSuccessStatusCode();//抛出异常

            return JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync(), context.ReturnType);
        }
    }
}
