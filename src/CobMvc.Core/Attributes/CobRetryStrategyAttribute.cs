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

        /// <summary>
        /// 重试次数，默认3次
        /// </summary>
        public int Count { get; set; } = 3;

        /// <summary>
        /// 失败后回退的默认值
        /// </summary>
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
