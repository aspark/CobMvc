using CobMvc.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.WebSockets
{
    class CobWebSocketClient : CobRequestBase
    {
        private ClientWebSocketPool _clientWebSocketPool = null;

        public CobWebSocketClient(ClientWebSocketPool clientWebSocketPool)
        {
            _clientWebSocketPool = clientWebSocketPool;
        }

        protected override async Task<object> DoRequest(CobRequestContext context, Type realType, object state)
        {
            var client = _clientWebSocketPool.GetOrCreate(context.Url);

            var timeout = Task.Delay(TimeSpan.FromSeconds(30));//todo:30s超时可配置

            var send = client.Send(new JsonRpcRequest { Method = new Uri(context.Url).PathAndQuery, Params = context.Parameters });

            if(await Task.WhenAny(timeout, send) == timeout)
            {
                throw new TimeoutException();
            }

            if (send.Result.Error == null)
            {
                if (send.Result.Result is JToken)
                {
                    //return JsonConvert.PopulateObject()
                    return (send.Result.Result as JToken).ToObject(context.ReturnType);
                }

                return null;
            }

            throw new Exception(send.Result.Error.Message);
        }
    }
}
