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
                return (Guid)GetOrAddProperty(CobwebDefaults.HeaderTraceID, () => Guid.NewGuid());
            }
            set
            {
                SetProperty(CobwebDefaults.HeaderTraceID, value);
            }
        }

        private ConcurrentDictionary<string, AsyncLocal<object>> _item = new ConcurrentDictionary<string, AsyncLocal<object>>();

        private object GetOrAddProperty(string key, Func<Object> creator)
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
    }

    public interface ICobwebContextAccessor
    {
        CobwebContext Current { get; }
    }

    public class CobwebContextAccessor : ICobwebContextAccessor
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
