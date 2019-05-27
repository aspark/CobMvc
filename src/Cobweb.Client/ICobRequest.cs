using Cobweb.Core.Service;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Client
{
    public interface ICobRequest
    {
        Task<object> DoRequest(CobRequestContext context, object[] states);
    }

    public class CobRequestContext
    {
        public string Url { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        public Type ReturnType { get; set; }

        /// <summary>
        /// 仅使用接口时有效
        /// </summary>
        public MethodInfo Method { get; set; }
    }

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
            throw new NotImplementedException();
        }

        public Task Get()
        {
            throw new NotImplementedException();
        }

        public Task Post()
        {
            throw new NotImplementedException();
        }
    }
}
