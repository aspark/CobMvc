using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Client
{
    public interface ICobClientFactory
    {
        T GetProxy<T>() where T : class;

        ICobClient GetProxy(CobServiceDescriptor desc);
    }
}
