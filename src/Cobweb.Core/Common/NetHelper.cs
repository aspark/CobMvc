using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cobweb.Core.Common
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
    }
}
