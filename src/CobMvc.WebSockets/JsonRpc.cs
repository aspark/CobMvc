using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.WebSockets
{
    internal abstract class JsonRpcBase
    {
        [JsonProperty("id")]
        public Guid ID { get; set; } = Guid.NewGuid();

        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("params")]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    internal class JsonRpcRequest : JsonRpcBase
    {
        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }

    }

    internal class JsonRpcResponse : JsonRpcBase
    {
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }
    }

    internal class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }

    }

    internal static class JsonRpcMessages
    {
        public static JsonRpcRequest PingRequest = new JsonRpcRequest() { ID = Guid.Empty, Method = "rpc.ping" };

        public static JsonRpcResponse PongResponse = new JsonRpcResponse() { ID = PingRequest.ID, Result = "rpc.ping" };

        public static JsonRpcResponse CreateError(string msg)
        {
            return new JsonRpcResponse()
            {
                Error = new JsonRpcError()
                {
                    Code = 500,
                    Message = msg
                }
            };
        }

        public static JsonRpcResponse CreateError(Guid id, string msg)
        {
            return CreateError(id, 500, msg);
        }

        public static JsonRpcResponse CreateError(Guid id, int code, string msg)
        {
            var ret = CreateError(msg);
            ret.ID = id;
            ret.Error.Code = code;

            return ret;
        }
    }


}
