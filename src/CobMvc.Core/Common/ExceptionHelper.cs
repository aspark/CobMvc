using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core.Common
{
    public static class ExceptionHelper
    {
        public static Exception GetInnerException(this Exception ex)
        {
            if (ex == null)
                return ex;

            while(ex.InnerException!=null)
            {
                ex = ex.InnerException;
            }

            return ex;
        }
    }
}
