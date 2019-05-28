using Cobweb.Core;
using Cobweb.Core.Service;
using System;
using System.Collections.Generic;
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
