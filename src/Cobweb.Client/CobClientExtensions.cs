using Cobweb.Core;
using Cobweb.Core.Client;
using Cobweb.Core.InMemory;
using Cobweb.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cobweb.Client
{
    public static class CobClientExtensions
    {
        public static void AddCobweb(this IServiceCollection services, Action<ICobweb> setup)
        {
            services.ConfigureClient();

            var container = new DefaultCobweb(services);

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
