﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CobMvc.Core.Common
{
    public class UriHelper
    {
        /// <summary>
        /// 暂不支持..上级
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string Combine(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a))
                return b ?? string.Empty;
            else if (string.IsNullOrWhiteSpace(b))
                return a;
            else
            {
                if(b.StartsWith(".."))
                {
                    throw new NotSupportedException();
                }

                if (b[0] == '/')
                {
                    if(Uri.IsWellFormedUriString(a, UriKind.Absolute))
                    {
                        return new Uri(new Uri(a), b).ToString();
                    }

                    return b;
                }
                else
                {
                    var path = a;
                    if (path[a.Length - 1] != '/')
                        path += '/';

                    path += b;

                    return path;
                }
            }
        }

        public static string Combine(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            return paths.Aggregate(string.Empty, (a, b) => Combine(a, b));
        }
   }
}
