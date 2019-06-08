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
            throw new NotImplementedException();
        }

        public Task<bool> Register(ServiceInfo entry)
        {
            _services.AddOrUpdate(entry.ID, entry, (id, e) => entry);

            return Task.FromResult(true);
        }

        public Task<bool> SetStatus(string id, ServiceInfoStatus status)
        {
            throw new NotImplementedException();
        }
    }
}
