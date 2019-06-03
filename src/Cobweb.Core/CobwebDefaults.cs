using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core
{
    public class CobwebDefaults
    {
        public const string UserAgent = "cobweb";

        private const string HeaderPrefix = "x-cobweb-";

        public const string HeaderTraceID = HeaderPrefix + "traceid";
    }
}
