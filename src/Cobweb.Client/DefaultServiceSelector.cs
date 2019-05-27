using Cobweb.Core.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cobweb.Client
{
    //服务路由
    public class DefaultServiceSelector : ICobServiceSelector
    {
        ConcurrentDictionary<string, ServiceInfoStatus> _services = new ConcurrentDictionary<string, ServiceInfoStatus>();
        IServiceRegistration _serviceDiscovery;
        string _serviceName;


        public DefaultServiceSelector(IServiceRegistration serviceDiscovery, string serviceName)
        {
            _serviceDiscovery = serviceDiscovery;
            _serviceName = serviceName;
        }

        private int _init = 0;
        private void EnsureInit()
        {
            if (Interlocked.CompareExchange(ref _init, 1, 0) == 0)
            {
                var task = Refresh();

                task.Wait();
            }
        }

        //todo:定时刷新
        private async Task Refresh()
        {
            var services = await _serviceDiscovery.GetByName(_serviceName);
            foreach (var svc in services)
            {
                _services.GetOrAdd(svc.ID, id => new ServiceInfoStatus(svc)).Service = svc;
            }

            Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(t => Refresh().Wait());
        }

        private int _currentServiceIndex = -1;
        //负载
        public ServiceInfo GetOne()
        {
            EnsureInit();

            var services = _services.Values.ToArray();

            ServiceInfo target = null;

            //round robin
            for (var i = 0; i < services.Length; i++)
            {
                Interlocked.Increment(ref _currentServiceIndex);
                var index = (_currentServiceIndex + i) % services.Length;
                if (services[index].Service.Status == Core.Service.ServiceInfoStatus.Healthy)
                {
                    services[index].RequestCount++;
                    target = services[index].Service;

                    break;
                }
            }

            //todo:最小压力


            return target;
        }

        public void IncreaseFailedCount(ServiceInfo service)
        {
            if (_services.TryGetValue(service.ID, out ServiceInfoStatus status))
            {
                status.FailedCount++;
            }
        }

        public void SetResponseTime(ServiceInfo service, TimeSpan time)
        {
            if (_services.TryGetValue(service.ID, out ServiceInfoStatus status))
            {
                status.ResponseTime = time.Ticks;
            }
        }

        private class ServiceInfoStatus
        {
            public ServiceInfoStatus(ServiceInfo service)
            {
                Service = service;
            }

            public ServiceInfo Service { get; set; }

            public volatile int FailedCount = 0;

            /// <summary>
            /// 响应时间
            /// </summary>
            public long ResponseTime { get; set; }

            /// <summary>
            /// 请求次数
            /// </summary>
            public long RequestCount;
        }
    }
}
