using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CobMvc.Core.Client
{
    public interface ICobRequestFilter
    {
        //todo:scope service/action

        int Order { get; }

        void OnBeforeRequest(CobRequestContext context);


        //void OnAfterRequest();


        void OnAfterResponse(MethodInfo method, object result);
    }
}
