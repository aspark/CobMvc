using Cobweb.Core.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Client
{
    public interface ICobServiceSelector
    {
        ServiceInfo GetOne();

        void SetServiceResponseTime(ServiceInfo service, TimeSpan time);

        void SetServiceFailed(ServiceInfo service);
    }
}
