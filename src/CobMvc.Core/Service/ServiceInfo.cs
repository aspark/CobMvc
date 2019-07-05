using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Service
{
    /// <summary>
    /// 服务信息
    /// </summary>
    public class ServiceInfo
    {
        public string ID { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 服务根地址
        /// </summary>
        public string Address { get; set; }

        //Address包含了所有信息，不需要再单独使用port
        //public int Port { get; set; }

        public ServiceInfoStatus Status { get; set; }
        
        public ServiceCheckInfo[] CheckInfoes { get; set; }
    }

    public enum ServiceInfoStatus
    {
        Critical,

        Warning,

        Healthy
    }
}
