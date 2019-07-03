using CobMvc.Core.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Client
{
    public interface ICobServiceSelector
    {
        ServiceInfo GetOne();

        void MarkServiceHealthy(ServiceInfo service, TimeSpan responseTime);

        void MarkServiceFailed(ServiceInfo service, bool notifyRegistry);
    }
}
