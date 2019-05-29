using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple =false)]
    public class CobServiceAttribute : Attribute
    {
        public CobServiceAttribute()
        {

        }

        public CobServiceAttribute(string serviceName)
        {
            ServiceName = serviceName;
        }

        public string ServiceName { get; set; }

        public string Path { get; set; }
    }

}
