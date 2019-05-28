using Cobweb.Client;
using Cobweb.Core;
using Cobweb.Core.Client;
using Cobweb.Core.InMemory;
using Cobweb.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb
{
    internal class ServicesExtensions
    {

        public static void RegisterDefaultServices(IServiceCollection services)
        {
            services.AddTransient<CobwebMiddleware>();

            services.TryAddSingleton<IServiceRegistration, InMemoryServiceRegistration>();

            services.ConfigureClient();
        }

    }
}
