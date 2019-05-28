using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Client
{
    public interface ICobClientFactory
    {
        T GetProxy<T>(CobClientOptions options) where T : class;

        ICobClient GetProxy(string serviceName, Dictionary<string, object> parameters, params object[] states);
    }
}
