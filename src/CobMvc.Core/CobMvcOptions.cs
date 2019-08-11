using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class CobMvcOptions
    {
        public string ServiceName { get; set; }

        /// <summary>
        /// 服务地址，可带路径，如果使用的是Consul路径部分以Tag的方式处理
        /// </summary>
        public string ServiceAddress { get; set; }

        //public int Port { get; set; }

        public string HealthCheck { get; set; }

        /// <summary>
        /// 最大调用次数，以免循环调用，默认10
        /// </summary>
        public int MaxJump { get; set; } = 10;

        /// <summary>
        /// 使用自定义的参数绑定，以支持多参数action
        /// </summary>
        public bool UseCobMvcModelBinder { get; set; } = false;

        /// <summary>
        /// 将服务名替换为服务发现中的Host，默认为true。如需使用sidecar等代理模式请设置为false
        /// </summary>
        public bool ResolveServiceName { get; set; } = true;
    }

    public class CobMvcHttpOptions
    {
        /// <summary>
        /// http最大连接数，默认 200
        /// </summary>
        public int MaxConnetions { get; set; } = 200;
    }
}
