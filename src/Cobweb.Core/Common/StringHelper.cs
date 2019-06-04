using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Cobweb.Core.Common
{
    public static class StringHelper
    {
        //public static string ToHex(this string str)
        //{
        //    if (string.IsNullOrEmpty(str))
        //        return string.Empty;

        //    return Encoding.UTF8.GetBytes(str).ToHex();
        //}

        public static string ToHex(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        public static string ToMD5(this string str)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(Encoding.UTF8.GetBytes(str)).ToHex();
            }
        }
    }
}
