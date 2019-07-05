using CobMvc.Core.Service;
using Consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CobMvc.Consul
{
    internal class ConsulServiceRegistration : IServiceRegistration
    {
        ConsulClient _client = null;

        public ConsulServiceRegistration(Action<ConsulClientConfiguration> config)
        {
            _client = new ConsulClient(config);
        }

        public async Task<List<ServiceInfo>> GetAll()
        {
            var items = new List<ServiceInfo>();
            var services = await _client.Agent.Services();
            if(services.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var status = (await _client.Health.State(HealthStatus.Any)).Response.GroupBy(h => h.ServiceID).ToDictionary(g => g.Key, g => new HashSet<HealthStatus>(g.Select(s => s.Status)));
                foreach (var svc in services.Response.Values)
                {
                    items.Add(CreateServiceInfo(svc.ID, svc.Service, svc.Address, svc.Port, svc.Tags, status));
                }
            }

            return items;
        }

        public async Task<List<ServiceInfo>> GetByName(string name)
        {
            var items = new List<ServiceInfo>();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var services = await _client.Catalog.Service(name);
                if (services.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var status = (await _client.Health.State(HealthStatus.Any)).Response.GroupBy(h => h.ServiceID).ToDictionary(g => g.Key, g => new HashSet<HealthStatus>(g.Select(s => s.Status)));
                    foreach (var svc in services.Response)
                    {
                        items.Add(CreateServiceInfo(svc.ServiceID, svc.ServiceName, svc.ServiceAddress, svc.ServicePort, svc.ServiceTags, status));
                    }
                }
            }

            return items;
        }

        private ServiceInfo CreateServiceInfo(string id, string name, string host, int port, string[] tags, Dictionary<string, HashSet<HealthStatus>> status)
        {
            var addr = new UriBuilder(host);
            if (tags != null && tags.Length > 0)
            {
                foreach(var tag in tags)
                {
                    if (tag.StartsWith(_prefixScheme))
                        addr.Scheme = tag.Substring(_prefixScheme.Length);
                    else if(tag.StartsWith(_prefixAbsolutePath))
                        addr.Path = tag.Substring(_prefixAbsolutePath.Length);
                }
            }
            else
            {
                //设置默认schema
                addr.Scheme = "http";
            }

            addr.Port = port;

            var svc = new ServiceInfo
            {
                ID = id,
                Name = name,
                Address = addr.ToString(),
                Status = status.ContainsKey(id) ? IndicateStatus(status[id]) : ServiceInfoStatus.Healthy//没有健康检查时，默认为健康
            };

            return svc;
        }

        private ServiceInfoStatus IndicateStatus(HashSet<HealthStatus> set)
        {
            if (set != null)
            {
                if (set.Contains(HealthStatus.Critical))
                    return ServiceInfoStatus.Critical;
                else if (set.Contains(HealthStatus.Warning))
                    return ServiceInfoStatus.Warning;

                return ServiceInfoStatus.Healthy;
            }

            return ServiceInfoStatus.Warning;
        }

        private const string _prefixScheme = "Scheme:";
        private const string _prefixAbsolutePath = "Path:";

        public async Task<bool> Register(ServiceInfo entry)
        {
            if(entry != null)
            {
                var url = new Uri(entry.Address);

                var registration = new AgentServiceRegistration
                {
                    Address = url.Host,
                    ID = entry.ID,
                    Name = entry.Name,
                    Port = url.Port,
                    Tags = new[] { $"{_prefixScheme}{url.Scheme}", $"{_prefixAbsolutePath}{url.AbsolutePath}" }
                };

                //健康检查
                if (entry.CheckInfoes != null && entry.CheckInfoes.Any())
                {
                    registration.Checks = entry.CheckInfoes.Select(i => {
                        var chk = new AgentServiceCheck
                        {
                            Interval = i.Interval,
                            Timeout = i.Timeout,
                            Status = HealthStatus.Passing,
#if DEBUG
                            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(10)
#else
                            DeregisterCriticalServiceAfter = TimeSpan.FromHours(1)
#endif
                        };

                        switch (i.Type)
                        {
                            case ServiceCheckInfoType.Tcp:
                                chk.TCP = $"{i.Target.Host}:{i.Target.Port}";
                                break;
                            case ServiceCheckInfoType.Ping:
                                chk.TCP = $"{i.Target.Host}:80";//not support
                                break;
                            default:
                            case ServiceCheckInfoType.Http:
                                chk.HTTP = i.Target.OriginalString;
                                break;
                        }

                        return chk;
                    }).ToArray();
                }

                var result = await _client.Agent.ServiceRegister(registration);

                return result.StatusCode == System.Net.HttpStatusCode.OK;
            }

            return false;
        }

        public async Task<bool> Deregister(string id)
        {
            var result = await _client.Agent.ServiceDeregister(id);

            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public Task<bool> SetStatus(string id, ServiceInfoStatus status)
        {
            return Task.FromResult(true);//todo:改变服务可用状态
        }
    }
}
