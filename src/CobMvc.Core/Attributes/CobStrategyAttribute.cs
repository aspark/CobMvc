using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CobStrategyAttribute : Attribute
    {
        public Type[] ExceptionTypes { get; set; } = new Type[0];

        public int RetryTimes { get; set; }

        public string FallbackValue { get; set; }
    }

}
