using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Core.Client
{
    /// <summary>
    /// 通用的客户端
    /// </summary>
    public interface ICobClient
    {
        T Invoke<T>(string name, Dictionary<string, object> parameters, object state);
    }

}
