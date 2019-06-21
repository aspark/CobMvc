using CobMvc.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using CobMvc.Core.Service;
using Consul;

namespace CobMvc.Consul
{
    public static class ConsulExtensions
    {
        public static ICobMvc AddConsul(this ICobMvc web, Action<ConsulClientConfiguration> option)
        {
            web.ConfigureServices(services =>
            {
                services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>(p => new ConsulServiceRegistration(option));
                //services.AddSingleton<ICobConfiguration, ConsulConfiguration>();
            });

            return web;
        }

        //public static void AddConsul(this IConfigurationBuilder builder)
        //{
        //    builder.Sources.Add()
        //}
    }
}
