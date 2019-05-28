using Cobweb.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using Cobweb.Core.Service;
using Consul;

namespace Cobweb.Consul
{
    public static class ConsulExtensions
    {
        public static void UseConsul(this ICobweb web, Action<ConsulClientConfiguration> option)
        {
            web.ConfigureServices(services =>
            {
                services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>(p => new ConsulServiceRegistration(option));
                //services.AddSingleton<ICobConfiguration, ConsulConfiguration>();
            });
        }
    }
}
