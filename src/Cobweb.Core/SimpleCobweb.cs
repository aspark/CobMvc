using Cobweb.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Core
{
    public class SimpleCobweb : ICobweb
    {
        public SimpleCobweb(IServiceCollection services)
        {
            _services = services;
        }

        private IServiceCollection _services = null;

        private List<Action<IServiceCollection>> _configServices = new List<Action<IServiceCollection>>();
        public void ConfigureServices(Action<IServiceCollection> services)
        {
            _configServices.Add(services);
        }

        public void ApplyConfigure()
        {
            _services.AddSingleton<ICobweb>(this);
            _services.AddOptions<CobwebOptions>();

            _configServices.ForEach(config => config(_services));
        }

        public void Configure<T>(Action<T> options) where T : class
        {
            //_configOptions.Add(options);
            _services.Configure(options);
        }

        public void ConfigureOptions(Action<CobwebOptions> options)
        {
            _services.Configure(options);
        }
    }
}
