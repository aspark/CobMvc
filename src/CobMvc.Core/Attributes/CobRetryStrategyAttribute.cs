using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CobMvc.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CobRetryStrategyAttribute : Attribute
    {
        /// <summary>
        /// 需要处理的异常。为空时全部处理
        /// </summary>
        public Type[] Exceptions { get; set; } = new Type[0];

        public int Count { get; set; }

        public string FallbackValue { get; set; }

        /// <summary>
        /// 继承自<see cref="ICobFallbackHandler"/>的类
        /// </summary>
        public Type FallbackHandler { get; set; }
    }

    public interface ICobFallbackHandler
    {
        object GetValue(CobFallbackHandlerContext context);
    }

    public struct CobFallbackHandlerContext
    {
        public Type ReturnType { get; set; }

        public Exception Exception;

        public string ServiceName;

        public string Path { get; set; }

        /// <summary>
        /// 调用的方法
        /// </summary>
        public MethodInfo Method { get; set; }

        /// <summary>
        /// 调用的参数
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
    }
}
