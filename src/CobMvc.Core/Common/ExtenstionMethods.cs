using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Common
{
    public static class ExtenstionMethods
    {
        /// <summary>
        /// 值类型或字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsValueTypeOrString(this object obj)
        {
            if (obj != null)
            {
                var type = obj.GetType();

                return IsValueTypeOrString(type);
            }

            return false;
        }

        public static bool IsValueTypeOrString(this Type type)
        {
            if (type != null)
            {
                return type.IsValueType || type == typeof(string);
            }

            return false;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> callback)
        {
            if (items != null && callback != null)
            {
                foreach (var item in items)
                    callback(item);
            }

            return items;
        }
    }
}
