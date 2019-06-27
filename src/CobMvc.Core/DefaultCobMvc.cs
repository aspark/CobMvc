using CobMvc.Core;
using CobMvc.Core.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public class DefaultCobMvc : ICobMvc
    {
        public DefaultCobMvc(IServiceCollection services)
        {
            _services = services;

            //add default service
            _services.AddSingleton<ICobMvc>(this);
            _services.AddSingleton<ICobRequestResolver, DefaultCobRequestResolver>();
            _services.TryAddSingleton<ICobServiceDescriptorGenerator, CobServiceDescriptorGenerator>();
            _services.TryAddSingleton<ICobMvcContextAccessor, CobMvcContextAccessor>();
            _services.AddOptions<CobMvcOptions>();

        }

        private IServiceCollection _services = null;

        private List<Action<IServiceCollection>> _configServices = new List<Action<IServiceCollection>>();
        public void ConfigureServices(Action<IServiceCollection> services)
        {
            _configServices.Add(services);
        }

        public void ApplyConfigure()
        {
            _configServices.ForEach(config => config(_services));
        }

        public void Configure<T>(Action<T> options) where T : class
        {
            //_configOptions.Add(options);
            _services.Configure(options);
        }

        public void ConfigureOptions(Action<CobMvcOptions> options)
        {
            _services.Configure(options);
        }
    }
}
