using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Core
{
    public interface ICobRequest
    {
        Task<object> DoRequest(CobRequestContext context, object state);
    }

    public class CobRequestContext
    {
        public string Url { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        //public object Body { get; set; }

        /// <summary>
        /// 非Task,如果为void非为null
        /// </summary>
        public Type ReturnType { get; set; }
    }

    /// <summary>
    /// 使用接口生成的调用上下文
    /// </summary>
    public class TypedCobRequestContext : CobRequestContext
    {
        /// <summary>
        /// 
        /// </summary>
        public MethodInfo Method { get; set; }
    }
}
