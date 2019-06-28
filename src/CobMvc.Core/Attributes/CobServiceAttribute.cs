using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple =false)]
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
        /// 调用路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 使用的传输类型，默认Http。可选：<see cref="CobRequestTransports"/>
        /// </summary>
        public string Transport { get; set; }
    }
}
