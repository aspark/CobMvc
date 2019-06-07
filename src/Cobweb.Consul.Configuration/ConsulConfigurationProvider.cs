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

                var data = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var pair in res.Response.SelectMany(p => ConvertValue(p)))
                {
                    data[pair.Key] = pair.Value;//can override older key
                }

                Data = data;

                if (isReload)
                    OnReload();
            }
            else
            {
                await Task.Delay(3000);
            }

            if(!_cts.IsCancellationRequested)
                LoadImpl(true).ConfigureAwait(false);

        }

        private List<KeyValuePair<string, string>> ConvertValue(KVPair pair)
        {
            var dic = new List<KeyValuePair<string, string>>();
            var val = Encoding.UTF8.GetString(pair.Value);

            var isJson = false;
            if (!string.IsNullOrWhiteSpace(val))
            {
                if((val[0] == '{' && val[val.Length - 1] == '}')|| (val[0] == '[' && val[val.Length - 1] == ']'))
                {
                    try
                    {
                        var obj = JContainer.Parse(val);
                        foreach(var item in FlattenJsonObject(pair.Key, obj))
                        {
                            if(ConvertToConfigurationKey(_parent.Root, item.path, out string key))
                                dic.Add(new KeyValuePair<string, string>(key, GetTokenString(item.token)));
                        }
                        isJson = true;
                    }
                    catch(Exception ex) { }
                }
            }

            if (!isJson)
            {
                if (ConvertToConfigurationKey(_parent.Root, pair.Key, out string key))
                    dic.Add(new KeyValuePair<string, string>(key, val));
            }

            return dic;
        }

        private string GetTokenString(JToken token)
        {
            if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
                return null;

            return token.Value<string>();
        }

        private IEnumerable<(string path, JToken token)> FlattenJsonObject(string path, JToken token)
        {
            switch (token)
            {
                case JObject obj:
                    foreach (var item in obj)
                    {
                        foreach (var prop in FlattenJsonObject(path + "/" + item.Key, item.Value))
                            yield return prop;
                    }
                    break;
                case JArray arr:
                    for (int index = 0; index < arr.Count; index++)
                    {
                        foreach (var prop in FlattenJsonObject(path + "/{index}", arr[index]))
                            yield return prop;
                    }
                    break;
                default:
                    yield return (path, token);//+ "/" + (token as JProperty).Name
                    break;
            }
        }

        private bool ConvertToConfigurationKey(string prefix, string key, out string configKey)
        {
            configKey = string.Empty;
            if (key.Length <= prefix.Length)
                return false;

            var result = new Span<char>(key.ToArray());
            var start = prefix.Length;
            while (result[start] == '/')
                start++;

            for (var i = start; i < result.Length; i++)
            {
                if (result[i] == '/')
                    result[i] = ':';
            }

            configKey = new string(result.Slice(start).ToArray());

            return true;
        }

        public void Dispose()
        {
            _cts.Cancel();
        }
    }
}
