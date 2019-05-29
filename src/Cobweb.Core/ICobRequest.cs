using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Core
{
    public interface ICobRequest
    {
        Task<object> DoRequest(CobRequestContext context, object[] states);
    }

    public class CobRequestContext
    {
        public string Url { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        //public object Body { get; set; }

        public Type ReturnType { get; set; }

        /// <summary>
        /// 仅使用接口时有效
        /// </summary>
        public MethodInfo Method { get; set; }
    }

}
