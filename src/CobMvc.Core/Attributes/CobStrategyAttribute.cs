using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobStrategyAttribute : Attribute
    {
        public Type ExceptionType { get; set; }

        public int RetryTimes { get; set; }

        //public string DefaultValue { get; set; }
    }

}
