using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Service
{
    /// <summary>
    /// 服务信息
    /// </summary>
    public class ServiceInfo
    {
        public string ID { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

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
