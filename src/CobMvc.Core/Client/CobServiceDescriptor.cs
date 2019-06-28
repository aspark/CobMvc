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

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 访问路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 使用的传输类型，默认使用Http。可选：<see cref="CobRequestTransports"/>
        /// </summary>
        public string Transport { get; set; }

        ///// <summary>
        ///// 编码方式，默认json
        ///// </summary>
        //public string Formatter { get; set; } = "Json";

        /// <summary>
        /// 超时时间
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int? Retry { get; set; }

        //public virtual string GetUrl(ServiceInfo service)
        //{
        //    return GetUrl(service.Address, Path);
        //}

        public virtual string GetUrl(ServiceInfo service, object action)
        {
            return GetUrl(service.Address, Path, action?.ToString());
        }

        protected string GetUrl(params string[] paths)
        {
            return string.Join("/", paths.Where(p => p != null).Select(p => p.Trim('/')));
        }

        //public virtual CobServiceDescriptor Clone()
        //{
        //    return new CobServiceDescriptor
        //    {
        //        ServiceName = ServiceName,
        //        Path = Path,
        //        Retry = Retry,
        //        Timeout = Timeout,
        //        Transport = Transport,
        //        //Formatter = Formatter
        //    };
        //}

        /// <summary>
        /// 使用<paramref name="refer"/>中的非空值覆盖当前值, 但Path为合并
        /// </summary>
        /// <param name="refer"></param>
        /// <returns></returns>
        public virtual CobServiceDescriptor Refer(CobServiceDescriptor refer)
        {
            AssignByValidValue(this.ServiceName, refer.ServiceName, v => ServiceName = v);

            AssignByValidValue(this.ServiceName, refer.ServiceName, v => Path = UriHelper.Combine(v, Path));

            AssignByValidValue(this.Retry, refer.Retry, v => Retry = v);
            AssignByValidValue(this.Timeout, refer.Timeout, v => Timeout = v);
            AssignByValidValue(this.Transport, refer.Transport, v => Transport = v);
            //AssignByValidValue(this.Formatter, refer.Formatter, v => Formatter = v);

            return this;
        }

        //public static void To(CobServiceDescriptor from)
        //{

        //}

        //private void AssignByValidValue<T>(T value, Action<T> setter)
        //{
        //    if (value is string && string.IsNullOrWhiteSpace(value?.ToString()))
        //    {
        //        return;
        //    }

        //    if (object.Equals(value, default(T)))
        //        return;

        //    setter(value);
        //}

        private void AssignByValidValue<P>(P original, P refer, Action<P> setter)
        {
            if(!HasValue(original) && HasValue(refer))
                setter(refer);
        }

        private bool HasValue<T>(T value)
        {
            if (value is string && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return false;
            }

            if (object.Equals(value, default(T)))
                return false;

            return true;
        }
    }

    /// <summary>
    /// 接口生成的服务调用 
    /// </summary>
    public class TypedCobServiceDescriptor : CobServiceDescriptor
    {
        public ConcurrentDictionary<MethodInfo, TypedCobActionDescriptor> ActionDescriptors { get; private set; } = new ConcurrentDictionary<MethodInfo, TypedCobActionDescriptor>();

        public CobServiceDescriptor GetActionDesc(MethodInfo action)
        {
            return ActionDescriptors.ContainsKey(action) ? ActionDescriptors[action] : this;
        }

        public string GetUrl(ServiceInfo service, object action, out CobServiceDescriptor actionOrTypeDesc)
        {
            actionOrTypeDesc = this;
            MethodInfo method = null;

            if (action is string)
            {
                return base.GetUrl(service, action);
            }
            else
                method = action as MethodInfo;

            if (method == null)
            {
                throw new InvalidOperationException($"can not find the methodinfo from:{action}");
            }

            if(ActionDescriptors.ContainsKey(method))
            {
                actionOrTypeDesc = ActionDescriptors[method];
                
                return ActionDescriptors[method].GetUrl(service);
            }

            return base.GetUrl(service, method.Name);
        }

        public override string GetUrl(ServiceInfo service, object action)
        {
            return GetUrl(service, action, out _);
        }
    }

    /// <summary>
    /// 方法描述
    /// </summary>
    public class TypedCobActionDescriptor : TypedCobServiceDescriptor
    {
        public CobServiceDescriptor Parent { get; internal set; }
        public MethodInfo Method { get; internal set; }

        public string GetUrl(ServiceInfo service)
        {
            var skipMethodName = !string.IsNullOrWhiteSpace(Path);

            return skipMethodName ? base.GetUrl(service.Address, Path) : Parent.GetUrl(service, Method.Name);

        }

        public override CobServiceDescriptor Refer(CobServiceDescriptor refer)
        {
            this.Parent = refer;

            return base.Refer(refer);
        }
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
                    //合并方法与全局配置
                    item.Refer(global);
                    item.Method = method;
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
                desc.Transport = serviceAttr.Transport;

                return true;
            }

            return false;
        }
    }
}
