using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobCacheAttribute : Attribute
    {
        public bool Enable { get; set; }
    }

}
