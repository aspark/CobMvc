using CobMvc.Core.Common;
using CobMvc.Core.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CobMvc.Core.Client
{
    /// <summary>
    /// 通用服务调用描述
    /// </summary>
    public class CobServiceDescriptor// : ICloneable
    {
        public CobServiceDescriptor()
        {

        }

        public string ServiceName { get; set; }

        public string Path { get; set; }

        public TimeSpan? Timeout { get; set; }

        public int? Retry { get; set; }

        public virtual string GetUrl(ServiceInfo service, object action)
        {
            return GetUrl(service.Address, Path, action?.ToString());
        }

        protected string GetUrl(params string[] paths)
        {
            return string.Join("/", paths.Select(p => p.Trim('/')));
        }

        public virtual CobServiceDescriptor Clone()
        {
            return new CobServiceDescriptor
            {
                ServiceName = ServiceName,
                Path = Path,
                Retry = Retry,
                Timeout = Timeout
            };
        }

        /// <summary>
        /// 使用<paramref name="from"/>中的非空值覆盖当前值, 但Path为合并
        /// </summary>
        /// <param name="from"></param>
        /// <returns></returns>
        public virtual CobServiceDescriptor Extend(CobServiceDescriptor from)
        {
            AssignByValidValue(from.ServiceName, v => ServiceName = v);

            AssignByValidValue(from.Path, v => {
                Path = UriHelper.Combine(Path, v);
            });

            AssignByValidValue(from.Retry, v => Retry = v);
            AssignByValidValue(from.Timeout, v => Timeout = v);

            return this;
        }

        private void AssignByValidValue<T>(T value, Action<T> setter)
        {
            if (value is string && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return;
            }

            if (object.Equals(value, default(T)))
                return;

            setter(value);
        }
    }

    /// <summary>
    /// 接口生成的服务调用 
    /// </summary>
    public class TypedCobServiceDescriptor : CobServiceDescriptor
    {
        public Dictionary<MethodInfo, TypedCobActionDescriptor> ActionDescriptors = new Dictionary<MethodInfo, TypedCobActionDescriptor>();

        public override string GetUrl(ServiceInfo service, object action)
        {
            MethodInfo method = null;
            if (action is string)
            {
                var methodName = action.ToString();
                method = ActionDescriptors.Keys.FirstOrDefault(f => string.Equals(f.Name, methodName, StringComparison.InvariantCultureIgnoreCase));
            }
            else
                method = action as MethodInfo;

            if(method == null)
            {
                throw new InvalidOperationException($"can not find the methodinfo from:{action}");
            }

            var desc = this.Clone();

            var skipMethodName = false;
            //合并方法与全局配置
            if (ActionDescriptors.ContainsKey(method))
            {
                skipMethodName = !string.IsNullOrWhiteSpace(ActionDescriptors[method].Path);
                desc.Extend(ActionDescriptors[method]);
            }

            return skipMethodName ? base.GetUrl(service.Address, desc.Path) : desc.GetUrl(service, method.Name);
        }
    }

    /// <summary>
    /// 方法描述
    /// </summary>
    public class TypedCobActionDescriptor : TypedCobServiceDescriptor
    {

    }

    public interface ICobServiceDescriptorGenerator
    {
        //TypedCobServiceDescriptor Create<T>() where T : class;

        TypedCobServiceDescriptor Create(Type type);
    }

    public class CobServiceDescriptorGenerator : ICobServiceDescriptorGenerator
    {

        private ConcurrentDictionary<Type, TypedCobServiceDescriptor> _serviceDesc = new ConcurrentDictionary<Type, TypedCobServiceDescriptor>();

        public TypedCobServiceDescriptor Create<T>() where T : class
        {
            return Create(typeof(T));
        }

        public TypedCobServiceDescriptor Create(Type type)
        {
            var desc = _serviceDesc.GetOrAdd(type, t => CreateImpl(t));

            return desc;
        }

        private TypedCobServiceDescriptor CreateImpl(Type targetType)
        {
            var attrs = targetType.GetCustomAttributes(false);

            //CobServiceAttribute
            ParseCobService(attrs, false, out TypedCobServiceDescriptor global);

            foreach(var method in targetType.GetMethods())
            {
                attrs = method.GetCustomAttributes(false);
                if(ParseCobService(attrs, true, out TypedCobActionDescriptor item) && item != null)
                {
                    global.ActionDescriptors[method] = item;
                }
            }

            return global;
        }

        private bool ParseCobService<T>(object[] attrs, bool allowMiss, out T desc) where T: TypedCobServiceDescriptor, new()
        {
            desc = null;
            var attr = attrs.FirstOrDefault(a => a is CobServiceAttribute);
            if (attr == null && !allowMiss)
            {
                throw new InvalidOperationException($"missing global CobServiceAttribute for interface/class");
            }

            if (attr != null)
            {
                var serviceAttr = attr as CobServiceAttribute;

                desc = new T();
                desc.ServiceName = serviceAttr.ServiceName;
                desc.Path = serviceAttr.Path;

                return true;
            }

            return false;
        }
    }
}
