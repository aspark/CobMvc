using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Service
{
    /// <summary>
    /// 服务健康度检查
    /// </summary>
    public class ServiceCheckInfo
    {
        public ServiceCheckInfoType Type { get; set; }

        public Uri Target { get; set; }

        public TimeSpan Interval { get; set; }

        public TimeSpan? Timeout { get; set; }
    }

    public enum ServiceCheckInfoType
    {
        /// <summary>
        /// Http Get
        /// </summary>
        Http,

        /// <summary>
        /// telnet
        /// </summary>
        Tcp,

        /// <summary>
        /// ping
        /// </summary>
        Ping
    }
}
