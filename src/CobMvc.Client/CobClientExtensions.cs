﻿using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.InMemory;
using CobMvc.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CobMvc.Client
{
    public static class CobClientExtensions
    {
        public static void AddCobMvc(this IServiceCollection services, Action<ICobMvc> setup)
        {
            services.ConfigureClient();

            var container = new DefaultCobMvc(services);

            setup?.Invoke(container);

            container.ApplyConfigure();

            ConfigureClient(services);
        }

        public static void ConfigureClient(this IServiceCollection services)
        {
            if(services is IEnumerable<ServiceDescriptor> items)
            {
                if (!items.Any(d => d.ServiceType == typeof(ILoggerFactory)))
                    services.AddLogging(builder=> {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Trace);
                    });
            }
            services.TryAddSingleton<IServiceRegistration, InMemoryServiceRegistration>();
            services.TryAddSingleton<ICobClientFactory, CobClientFactory>();
            services.TryAddSingleton<ICobRequest, HttpClientCobRequest>();
            services.TryAddSingleton<ICobServiceSelector, DefaultServiceSelector>();
        }
    }
}