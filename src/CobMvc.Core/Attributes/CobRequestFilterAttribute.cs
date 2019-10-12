using CobMvc.Core.Client;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CobMvc.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public abstract class CobRequestFilterAttribute : Attribute, ICobRequestFilter
    {
        public int Order { get; set; }

        public virtual void OnBeforeRequest(CobRequestContext context)
        {
            
        }

        public virtual void OnAfterResponse(MethodInfo method, object result)
        {
            
        }
    }
}
