using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobMvcDefaults
    {
        public const string HeaderUserAgent = "User-Agent";

        public const string UserAgentValue = "cobmvc";

        public const string HeaderUserVersion = "0.0.1";

        private const string HeaderPrefix = "x-cobmvc-";

        public const string HeaderTraceID = HeaderPrefix + "traceid";

        public const string HeaderJump = HeaderPrefix + "jump";
    }
}
