using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cobweb.Core
{
    public interface ICobwebContext
    {
        object GetProperty(string key);

        void SetProperty(string key, object value);

        Guid TraceID { get; }
    }

    public class CobwebContext : ICobwebContext
    {
        public CobwebContext()
        {
            
        }

        public Guid TraceID {
            get
            {
                return (Guid)GetOrAddItem(CobwebDefaults.HeaderTraceID, () => Guid.NewGuid());
            }
            set
            {
                SetProperty(CobwebDefaults.HeaderTraceID, value);
            }
        }

        private ConcurrentDictionary<string, AsyncLocal<object>> _item = new ConcurrentDictionary<string, AsyncLocal<object>>();

        private object GetOrAddItem(string key, Func<Object> creator)
        {
            var item = _item.GetOrAdd(key, k => new AsyncLocal<object> { Value = creator() });

            if (item.Value == null)
                item.Value = creator();
            
            return item.Value;
        }

        public object GetProperty(string key)
        {
            if (_item.TryGetValue(key, out AsyncLocal<object> val))
                return val.Value;

            return null;
        }

        public void SetProperty(string key, object value)
        {
            _item.AddOrUpdate(key, k => new AsyncLocal<object>() { Value = value }, (k, old) => { old.Value = value; return old; });
        }

        //public IDictionary<string, object> Items { get; } = new ContextItems();

        //private class ContextItems : IDictionary<string, object>
        //{
        //    private ConcurrentDictionary<string, AsyncLocal<object>> _item = new ConcurrentDictionary<string, AsyncLocal<object>>();

        //    public object this[string key] { get => _item[key].Value; set => _item[key].Value = value; }

        //    public ICollection<string> Keys => _item.Keys;

        //    public ICollection<object> Values => _item.Values.Select(v=>v.Value).ToList();

        //    public int Count => _item.Count;

        //    public bool IsReadOnly => false;

        //    public void Add(string key, object value)
        //    {
        //        _item.AddOrUpdate(key, new AsyncLocal<object>() {  Value =value}, (k,o)=> { o.Value = value; return o; });
        //    }

        //    public void Add(KeyValuePair<string, object> item)
        //    {
        //        Add(item.Key, item.Value);
        //    }

        //    public void Clear()
        //    {
        //        _item.Clear();
        //    }

        //    public bool Contains(KeyValuePair<string, object> item)
        //    {
        //        return ContainsKey(item.Key);
        //    }

        //    public bool ContainsKey(string key)
        //    {
        //        return _item.ContainsKey(key);
        //    }

        //    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        //    {
        //        return _item.Select(p => new KeyValuePair<string, object>(p.Key, p.Value.Value)).GetEnumerator();
        //    }

        //    public bool Remove(string key)
        //    {
        //        throw new NotImplementedException();
        //    }

        //    public bool Remove(KeyValuePair<string, object> item)
        //    {
        //        return _item.TryRemove(item.Key, out _);
        //    }

        //    public bool TryGetValue(string key, out object value)
        //    {
        //        value = null;
        //        var succ = _item.TryGetValue(key, out AsyncLocal<object> ret);
        //        if (succ)
        //            value = ret.Value;

        //        return succ;
        //    }

        //    IEnumerator IEnumerable.GetEnumerator()
        //    {
        //        return GetEnumerator();
        //    }
        //}
    }

    public interface ICobwebContextFactory
    {
        CobwebContext Current { get; }
    }

    public class CobwebContextFactory : ICobwebContextFactory
    {
        private static CobwebContext _current = new CobwebContext();
        public CobwebContext Current
        {
            get
            {
                return _current;
            }
        }
    }
}
