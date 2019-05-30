using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Core.Client
{
    public interface ICobClient
    {
        Task<T> Invoke<T>(string name, Dictionary<string, object> parameters, object state);
    }

}
