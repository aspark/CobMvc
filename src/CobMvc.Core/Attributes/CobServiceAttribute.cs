using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CobServiceAttribute : Attribute
    {
        public CobServiceAttribute()
        {

        }

        public CobServiceAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 将服务名替换为服务发现中的Host，默认为ResolveServiceName。如需使用sidecar等代理模式请设置为false；暂只支持http/https
        /// </summary>
        public EnumResolveServiceName ResolveServiceName { get; set; }

        /// <summary>
        /// 调用路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 使用的传输类型，默认Http。可选：<see cref="CobRequestTransports"/>
        /// </summary>
        public string Transport { get; set; }

        /// <summary>
        /// 超时时间（秒）。为0时使用全局设置
        /// </summary>
        public float Timeout { get; set; }
    }

    public enum EnumResolveServiceName
    {
        /// <summary>
        /// 处理为ResolveServiceName
        /// </summary>
        NotSet = 0,

        ResolveServiceName = 1,

        KeepServiceName = 2
    }
}
