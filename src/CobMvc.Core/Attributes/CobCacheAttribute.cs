using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobCacheAttribute : Attribute
    {
        public CobCacheAttribute()
        {

        }

        public bool Enable { get; set; } = true;
    }

}
