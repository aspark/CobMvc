using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cobweb.Consul.Configuration
{
    internal class ConsulConfigurationProvider : ConfigurationProvider, IDisposable
    {
        ConsulClient _client = null;
        ConsulConfigurationSource _parent = null;
        private CancellationTokenSource _cts = null;

        public ConsulConfigurationProvider(ConsulConfigurationSource parent, ConsulClient client)
        {
            _cts = new CancellationTokenSource();
            _client = client;
            _parent = parent;

            //GetReloadToken
            //ChangeToken.OnChange()
        }

        public override void Load()
        {
            LoadImpl(false).Wait();
        }

        private object _lastIndexLock = new object();
        private ulong _lastIndex = 0;
        private async Task LoadImpl(bool isReload)
        {
            var q = new QueryOptions() { WaitIndex = _lastIndex };
            var res = await _client.KV.List(_parent.Root, q, _cts.Token);
            if (res.StatusCode == System.Net.HttpStatusCode.OK)
            {
                lock (_lastIndexLock)
                {
                    _lastIndex = res.LastIndex;
                }

                var data = res.Response.SelectMany(p => ConvertValue(p)).ToDictionary(p => p.Key, p => p.Value);

                Data = data;

                if (isReload)
                    OnReload();
            }
            else
            {
                await Task.Delay(3000);
            }

            if(!_cts.IsCancellationRequested)
                await LoadImpl(true).ConfigureAwait(false);

        }

        private Dictionary<string, string> ConvertValue(KVPair pair)
        {
            var dic = new Dictionary<string, string>();
            var val = Encoding.UTF8.GetString(pair.Value);

            if (!string.IsNullOrWhiteSpace(val))
            {
                if((val[0] == '{' && val[val.Length - 1] == '}')|| (val[0] == '[' && val[val.Length - 1] == ']'))
                {
                    try
                    {
                        var obj = JContainer.Parse(val);
                        foreach(var item in FlattenJsonObject(_parent.Root, obj))
                        {
                            dic.Add(ConvertToConfigurationKey(_parent.Root, item.path), (string)item.token);
                        }
                    }
                    catch { }
                }
            }

            return dic;
        }

        private IEnumerable<(string path, JToken token)> FlattenJsonObject(string path, JToken token)
        {
            if (token is JObject obj)
            {
                foreach (var item in obj)
                {
                    foreach (var prop in FlattenJsonObject(path + "/" + item.Key, item.Value))
                        yield return prop;
                }
            }
            if (token is JArray arr)
            {
                for (int index = 0; index < arr.Count; index++)
                {
                    foreach (var prop in FlattenJsonObject(path + $"[{index}]", arr[index]))
                        yield return prop;
                }
            }
            else
                yield return (path, token);//+ "/" + (token as JProperty).Name
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

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
