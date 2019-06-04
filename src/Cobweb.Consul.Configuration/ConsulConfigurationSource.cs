using Consul;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cobweb.Consul.Configuration
{
    public class ConsulConfigurationSource : IConfigurationSource
    {
        Action<ConsulClientConfiguration> _config = null;
        ConsulClient _client = null;

        public string Root { get; set; } = "CobwebConfiguration";

        public ConsulConfigurationSource(Action<ConsulClientConfiguration> config)
        {
            _config = config;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            _client = new ConsulClient(_config);

            return new ConsulConfigurationProvider(this, _client);
        }
    }
}
