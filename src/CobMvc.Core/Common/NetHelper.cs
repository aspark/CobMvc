using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CobMvc.Core.Common
{
    public class NetHelper
    {
        public static int GetAvailablePort()
        {
            TcpListener net = null;
            try
            {
                net = new TcpListener(IPAddress.Loopback, 0);
                net.Start();

                return ((IPEndPoint)net.LocalEndpoint).Port;
            }
            finally
            {
                net?.Stop();
            }
        }

        public static IPAddress CetCurrentIP()
        {
            var addrList = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(a => !IPAddress.IsLoopback(a));

            var addr = addrList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if(addr == null)
                addr = addrList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6)?.MapToIPv4();

            return addr;
        }

        private static HashSet<string> _loopback = new HashSet<string> { "127.0.0.1", "localhost", "0.0.0.0", "*", "::1" };
        public static bool IsLoopBack(string host)
        {
            return _loopback.Contains(host);
        }

        /// <summary>
        /// 如果是localhost/127.0.0.1/::0/*/0.0.0.0，则需要改为本地ipv4
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ChangeToExternal(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            var uri = new Uri(url);

            if(IsLoopBack(uri.Host))
            {
                var builder = new UriBuilder(uri);
                builder.Host = NetHelper.CetCurrentIP()?.ToString() ?? uri.Host;
                uri = builder.Uri;
            }

            return uri.ToString();
        }
    }
}
