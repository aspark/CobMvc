using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobMvcDefaults
    {
        public const string UserAgent = "cobmvc";

        private const string HeaderPrefix = "x-cobmvc-";

        public const string HeaderTraceID = HeaderPrefix + "traceid";

        public const string HeaderJump = HeaderPrefix + "jump";
    }
}
