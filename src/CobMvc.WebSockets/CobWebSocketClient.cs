using CobMvc.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{
    class CobWebSocketClient : CobRequestBase
    {
        private ClientWebSocketPool _clientWebSocketPool = null;
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<CobWebSocketClient> _logger = null;

        public CobWebSocketClient(ICobMvcContextAccessor contextAccessor, ILogger<CobWebSocketClient> logger, ClientWebSocketPool clientWebSocketPool)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _clientWebSocketPool = clientWebSocketPool;
        }

        protected override async Task<object> DoRequest(CobRequestContext context, Type realType, object state)
        {
            var client = _clientWebSocketPool.GetOrCreate(context.Url);

            var timeout = Task.Delay(TimeSpan.FromSeconds(30));//todo:30s超时可配置

            var send = client.Send(ParseToRequest(context));

            if(await Task.WhenAny(timeout, send) == timeout)
            {
                throw new TimeoutException(client.ToString());
            }

            if (send.Result.Error == null)
            {
                if (send.Result.Result is JToken)
                {
                    //return JsonConvert.PopulateObject()
                    return (send.Result.Result as JToken).ToObject(realType);
                }

                return null;
            }

            throw new Exception(send.Result.Error.Message);
        }

        private JsonRpcRequest ParseToRequest(CobRequestContext context)
        {
            var request = new JsonRpcRequest();

            if (context is TypedCobRequestContext && ParseToRequest(context as TypedCobRequestContext, ref request))
            {
                return request;
            }

            request.Method = new Uri(context.Url).PathAndQuery;
            request.Params = context.Parameters;

            return request;
        }

        private bool ParseToRequest(TypedCobRequestContext context, ref JsonRpcRequest request)
        {
            var url = context.Url;

            var usePost = context.Method.GetParameters().Any(p => p.ParameterType.IsClass && p.ParameterType != typeof(string));
            var parameters = new Dictionary<string, object>(context.Parameters);
            if (!usePost)
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

                parameters.Clear();
            }

            request.Method = url;//new Uri(url).PathAndQuery
            request.Params = parameters;
            request.Properties.Add(CobMvcDefaults.UserAgent, "0.0.1");
            request.Properties.Add(CobMvcDefaults.HeaderTraceID, _contextAccessor.Current.TraceID.ToString());
            _logger?.LogDebug("set request traceID:{0}", _contextAccessor.Current.TraceID);

            return true;
        }
    }
}
