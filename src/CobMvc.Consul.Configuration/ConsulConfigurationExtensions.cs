using Consul;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Consul.Configuration
{
    public static class ConsulConfigurationExtensions
    {
        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, Action<ConsulClientConfiguration> config, string root)
        {
            return AddConsul(builder, config, src=> {
                src.Root = root;
            });
        }

        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, Action<ConsulClientConfiguration> config)
        {
            return AddConsul(builder, config, (Action<ConsulConfigurationSource>)null);
        }

        public static IConfigurationBuilder AddConsul(this IConfigurationBuilder builder, Action<ConsulClientConfiguration> config, Action<ConsulConfigurationSource> configSource)
        {
            var src = new ConsulConfigurationSource(config);

            configSource?.Invoke(src);

            builder.Add(src);

            return builder;
        }

    }
}
