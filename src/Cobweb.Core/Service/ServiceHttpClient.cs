using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Service
{
    public class ServiceHttpClient : IServiceClient
    {
        IServiceRegistration _serviceDiscovery = null;

        public ServiceHttpClient(IServiceRegistration serviceDiscovery)
        {
            _serviceDiscovery = serviceDiscovery;
        }

        public ServiceInfo Get(string serviceName)
        {
            throw new NotImplementedException();
        }

        public void Invoke()
        {

        }

        //服务路由

        //负载

        //熔断

        //重试
    }
}
