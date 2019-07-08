using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Attributes
{
    /// <summary>
    /// 从Configuration中加载配置
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
    public class CobServiceConfigAttribute : Attribute
    {
        public CobServiceConfigAttribute(string sectionKey)
        {
            SectionKey = sectionKey;
        }

        /// <summary>
        /// 
        /// </summary>
        public string SectionKey { get; set; }

    }
}
