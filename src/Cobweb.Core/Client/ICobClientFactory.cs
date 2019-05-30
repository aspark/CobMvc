using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Client
{
    public interface ICobClientFactory
    {
        T GetProxy<T>() where T : class;

        ICobClient GetProxy(CobServiceDescriptor desc);
    }
}
