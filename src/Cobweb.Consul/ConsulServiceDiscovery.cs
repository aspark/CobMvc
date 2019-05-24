using Cobweb.Core.Service;
using Consul;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Consul
{
    internal class ConsulServiceDiscovery : IServiceRegistration
    {
        ConsulClient _client = null;

        public ConsulServiceDiscovery(Action<ConsulClientConfiguration> config)
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
                    items.Add(new ServiceInfo
                    {
                        Host = svc.Address,
                        ID = svc.ID,
                        Name = svc.Service,
                        Port = svc.Port,
                        Status = status.ContainsKey(svc.ID)? IndicateStatus(status[svc.ID]): ServiceInfoStatus.Critical
                    });
                }
            }

            return items;
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
                        items.Add(new ServiceInfo
                        {
                            Host = svc.Address,
                            ID = svc.ServiceID,
                            Name = svc.ServiceName,
                            Port = svc.ServicePort,
                            Status = status.ContainsKey(svc.ServiceID) ? IndicateStatus(status[svc.ServiceID]) : ServiceInfoStatus.Critical
                        });
                    }
                }
            }

            return items;
        }

        public async Task<bool> Register(ServiceInfo entry)
        {
            if(entry != null)
            {
                var registration = new AgentServiceRegistration
                {
                    Address = entry.Host,
                    ID = entry.ID,
                    Name = entry.Name,
                    Port = entry.Port
                };

                //健康检查
                if (entry.CheckInfoes != null && entry.CheckInfoes.Any())
                {
                    registration.Checks = entry.CheckInfoes.Select(i => {
                        var chk = new AgentServiceCheck
                        {
                            Interval = i.Interval,
                            Timeout = i.Timeout,
                            Status = HealthStatus.Passing
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
    }
}
