using CobMvc.Client;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.InMemory;
using CobMvc.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc
{
    internal class ServicesExtensions
    {
        /// <summary>
        /// Mvc默认服务
        /// </summary>
        /// <param name="services"></param>
        public static void EnsureServerServices(IServiceCollection services)
        {
            services.AddTransient<CobMvcMiddleware>();

            services.TryAddSingleton<IServiceRegistration, InMemoryServiceRegistration>();

            services.EnsureClientServices();
        }

    }
}
