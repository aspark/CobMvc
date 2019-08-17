using CobMvc.Core.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Core.InMemory
{
    public class InMemoryServiceRegistration : IServiceRegistration
    {
        ConcurrentDictionary<string, ServiceInfo> _services = new ConcurrentDictionary<string, ServiceInfo>();

        public Task<bool> Deregister(string id)
        {
            return Task.FromResult(_services.TryRemove(id, out _));
        }

        public Task<List<ServiceInfo>> GetAll()
        {
            return Task.FromResult(_services.Values.ToList());
        }

        public Task<List<ServiceInfo>> GetByName(string name)
        {
            return Task.FromResult(_services.Values.Where(v => string.Equals(name, v.Name, StringComparison.InvariantCultureIgnoreCase)).ToList());
        }

        public Task<bool> Register(ServiceInfo entry)
        {
            _services.AddOrUpdate(entry.ID, entry, (id, e) => entry);

            return Task.FromResult(true);
        }

        public Task<bool> SetStatus(string id, ServiceInfoStatus status)
        {
            if(_services.TryGetValue(id, out ServiceInfo service))
            {
                service.Status = status;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}
