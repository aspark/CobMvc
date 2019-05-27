using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Client
{
    public class CobClientStrategyAttribute : Attribute
    {
        public Type ExceptionType { get; set; }

        public int RetryTimes { get; set; }

        //public string DefaultValue { get; set; }
    }

}
