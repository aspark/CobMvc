using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core.Client
{
    public class CobServiceDescription
    {
        public string ServiceName { get; set; }

        public string Path { get; set; }

        public TimeSpan Timeout { get; set; }

        public int Retry { get; set; }
    }
}
