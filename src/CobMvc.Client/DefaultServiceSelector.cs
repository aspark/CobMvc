using CobMvc.Core.Client;
using CobMvc.Core.Common;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.Client
{
    //服务选择/路由
    internal class DefaultServiceSelector : ICobServiceSelector
    {
        ConcurrentDictionary<string, ServiceInfoBag> _services = new ConcurrentDictionary<string, ServiceInfoBag>();
        IServiceRegistration _serviceRegistration;
        string _serviceName;
        ILogger<DefaultServiceSelector> _logger;

        public DefaultServiceSelector(IServiceRegistration serviceDiscovery, string serviceName, ILogger<DefaultServiceSelector> logger)
        {
            _logger = logger;
            _serviceRegistration = serviceDiscovery;
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

        //定时刷新
        private async Task Refresh()
        {
            var hasError = false;
            try
            {
                var services = await _serviceRegistration.GetByName(_serviceName);
                _logger?.LogDebug("find {0} servcies for {1}", services.Count, _serviceName);

                var exists = new HashSet<string>();
                foreach (var svc in services)
                {
                    _services.GetOrAdd(svc.ID, id => new ServiceInfoBag(svc)).Service = svc;//更新service状态
                    exists.Add(svc.ID);
                }

                //移除不存在的服务
                var saved = _services.Keys;
                foreach (var del in saved.Where(s => !exists.Contains(s)))
                {
                    _services.TryRemove(del, out _);
                }
            }
            catch(Exception ex)
            {
                hasError = true;
                _logger.LogError(ex, "service refresh failed");
            }

            Task.Delay(hasError ? TimeSpan.FromMinutes(1) : TimeSpan.FromSeconds(3)).ContinueWith(t => Refresh().Wait());//todo:时间可配置
        }

        private int _currentServiceIndex = -1;
        //负载
        public ServiceInfo GetOne()
        {
            EnsureInit();

            var services = _services.Values.ToArray();

            ServiceInfo target = null;

            Interlocked.CompareExchange(ref _currentServiceIndex, -1, int.MaxValue);
            //round robin
            for (var i = 0; i < services.Length; i++)
            {
                Interlocked.Increment(ref _currentServiceIndex);
                var index = (_currentServiceIndex) % services.Length;
                if (services[index].HasFailed.Value == false && services[index].Service.Status == Core.Service.ServiceInfoStatus.Healthy)
                {
                    services[index].RequestCount++;
                    target = services[index].Service;
                    _logger?.LogDebug("select service:{0}", index);

                    break;
                }
            }

            //todo:最小压力


            return target;
        }


        public void MarkServiceFailed(ServiceInfo service, bool notifyRegistry)
        {
            if (_services.TryGetValue(service.ID, out ServiceInfoBag status))
            {
                status.HasFailed.Set();

                if (notifyRegistry || status.HasFailed.IsExceeded)//异常超出阈值
                    _serviceRegistration.SetStatus(service.ID, Core.Service.ServiceInfoStatus.Warning);//是否需要改变注册中心的状态?
            }
        }

        public void MarkServiceHealthy(ServiceInfo service, TimeSpan time)
        {
            if (_services.TryGetValue(service.ID, out ServiceInfoBag status))
            {
                status.HasFailed.Reset();
                status.ResponseTime = time.Ticks;
            }
        }

        private class ServiceInfoBag
        {
            public ServiceInfoBag(ServiceInfo service)
            {
                HasFailed = new ThresholdAutoResetSignal(3, 500);//3次错误才标记为异常状态，每500/1000/2000...恢复一次
                Service = service;
            }

            public ServiceInfo Service { get; set; }

            public ThresholdAutoResetSignal HasFailed { get; private set; }

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
