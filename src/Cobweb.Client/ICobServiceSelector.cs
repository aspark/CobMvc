using Cobweb.Core.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Client
{
    public interface ICobServiceSelector
    {
        ServiceInfo GetOne();

        void SetResponseTime(ServiceInfo service, TimeSpan time);

        void IncreaseFailedCount(ServiceInfo service);
    }
}
