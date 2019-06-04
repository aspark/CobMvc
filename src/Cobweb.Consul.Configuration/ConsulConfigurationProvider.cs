using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Consul.Configuration
{
    internal class ConsulConfigurationProvider : ConfigurationProvider
    {
        ConsulClient _client = null;
        ConsulConfigurationSource _parent = null;

        public ConsulConfigurationProvider(ConsulConfigurationSource parent, ConsulClient client)
        {
            _client = client;
            _parent = parent;

            //GetReloadToken
            //ChangeToken.OnChange()
        }

        public override void Load()
        {
            LoadImpl(false).Wait();
        }

        //private 

        private object _lastIndexLock = new object();
        private ulong _lastIndex = 0;
        private async Task LoadImpl(bool isReload)
        {
            var q = new QueryOptions() { WaitIndex = _lastIndex };
            var res = await _client.KV.List(_parent.Root, q);
            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                lock (_lastIndexLock)
                {
                    _lastIndex = res.LastIndex;
                }

                var data = res.Response.ToDictionary(p => ConvertToConfigurationKey(_parent.Root, p.Key), p => Encoding.UTF8.GetString(p.Value));

                Data = data;

                if (isReload)
                    OnReload();
            }
            else
            {
                await Task.Delay(3000);
            }

            await LoadImpl(true).ConfigureAwait(false);

        }

        private string ConvertToConfigurationKey(string prefix, string key)
        {
            var result = new Span<char>(key.ToArray());
            var start = prefix.Length;
            while (result[start] == '/')
                start++;

            for (var i = start; i < result.Length; i++)
            {
                if (result[i] == '/')
                    result[i] = ':';
            }

            return new string(result.Slice(start).ToArray());
        }
    }
}
