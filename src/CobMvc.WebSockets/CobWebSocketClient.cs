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

            var result = await client.Send(new JsonRpcRequest { Method = new Uri(context.Url).AbsolutePath, Params = context.Parameters });

            if (result.Error == null)
            {
                if (result.Result is JToken)
                {
                    //return JsonConvert.PopulateObject()
                    return (result.Result as JToken).ToObject(context.ReturnType);
                }

                return null;
            }

            throw new Exception(result.Error.Message);
        }
    }
}
