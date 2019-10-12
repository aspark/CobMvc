using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    /// <summary>
    /// 从Configuration中加载配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class CobServiceFromConfigAttribute : Attribute
    {
        public CobServiceFromConfigAttribute(string sectionKey)
        {
            SectionKey = sectionKey;
        }

        /// <summary>
        /// 
        /// </summary>
        public string SectionKey { get; set; }

    }
}
