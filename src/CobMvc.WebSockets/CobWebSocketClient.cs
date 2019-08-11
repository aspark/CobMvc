using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Common;
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
    internal class CobWebSocketClient : CobRequestBase
    {
        private ClientWebSocketPoolFactory _clientWebSocketPoolFactory = null;
        ICobMvcContextAccessor _contextAccessor = null;
        ILogger<CobWebSocketClient> _logger = null;

        public CobWebSocketClient(ICobMvcContextAccessor contextAccessor, ILogger<CobWebSocketClient> logger, ClientWebSocketPoolFactory clientWebSocketPoolFactory)
        {
            _logger = logger;
            _contextAccessor = contextAccessor;
            _clientWebSocketPoolFactory = clientWebSocketPoolFactory;
        }

        public override string SupportTransport { get => CobRequestTransports.WebSocket; }

        protected override async Task<object> DoRequest(CobRequestContext context, Type realType, object state)
        {
            var client = _clientWebSocketPoolFactory.GetOrCreate(context).Get();

            var send = await client.Send(ParseToRequest(context));

            if (send.Error == null)
            {
                if (send.Result is JToken && realType != null)
                {
                    //return JsonConvert.PopulateObject()
                    return (send.Result as JToken).ToObject(realType);
                }

                return null;
            }

            throw new Exception(send.Error.Message);
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

            var usePost = context.Method.GetParameters().Any(p => !p.ParameterType.IsValueTypeOrString());
            var parameters = new Dictionary<string, object>(context.Parameters);
            if (context.Parameters != null && context.Parameters.Any())
            {
                var queries = context.Parameters.Where(p => p.Value != null && p.Value.IsValueTypeOrString()).ToArray();
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

            request.Method = url;//new Uri(url).PathAndQuery
            request.Params = parameters;
            request.Properties.Add(CobMvcDefaults.UserAgentValue, $"{CobMvcDefaults.HeaderUserAgent}/{CobMvcDefaults.HeaderUserVersion}");
            request.Properties.Add(CobMvcDefaults.HeaderTraceID, _contextAccessor.Current.TraceID.ToString());
            request.Properties.Add(CobMvcDefaults.HeaderJump, (_contextAccessor.Current.Jump + 1).ToString());
            _logger?.LogDebug("set request traceID:{0}", _contextAccessor.Current.TraceID);

            return true;
        }

        public override string GetDebugInfo()
        {
            return _clientWebSocketPoolFactory.GetDebugInfo();
        }
    }
}
