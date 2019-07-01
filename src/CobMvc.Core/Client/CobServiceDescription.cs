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
    public class CobServiceDescription// : ICloneable
    {
        public CobServiceDescription()
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
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryTimes { get; set; }

        /// <summary>
        /// 失败回退的值
        /// </summary>
        public string FallbackValue { get; set; }

        /// <summary>
        /// 重试异常
        /// </summary>
        public Type[] RetryExceptionTypes { get; set; }

        /// <summary>
        /// 根据服务获取调用的地址
        /// </summary>
        /// <param name="service"></param>
        /// <param name="action">方法名</param>
        /// <returns></returns>
        public virtual string GetUrl(ServiceInfo service, string action)
        {
            return GetUrl(service.Address, Path, action);//?.ToString() ?? ""
        }

        protected string GetUrl(params string[] paths)
        {
            return string.Join("/", paths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim('/')));
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
        /// 使用<paramref name="refer"/>中的非空值覆盖当前值, 但Path/RetryExceptionTypes为合并
        /// </summary>
        /// <param name="refer"></param>
        /// <returns></returns>
        public virtual CobServiceDescription Refer(CobServiceDescription refer)
        {
            AssignByValidValue(this.ServiceName, refer.ServiceName, v => ServiceName = v);

            if (HasValue(Path) && HasValue(refer.Path))
                Path = UriHelper.Combine(refer.Path, Path);// AssignByValidValue(this.Path, refer.Path, v => );

            AssignByValidValue(this.RetryTimes, refer.RetryTimes, v => RetryTimes = v);
            AssignByValidValue(this.Timeout, refer.Timeout, v => Timeout = v);
            AssignByValidValue(this.Transport, refer.Transport, v => Transport = v);
            //AssignByValidValue(this.Formatter, refer.Formatter, v => Formatter = v);
            AssignByValidValue(this.FallbackValue, refer.FallbackValue, v => FallbackValue = v);
            if (HasValue(refer.RetryExceptionTypes))
                this.RetryExceptionTypes = (this.RetryExceptionTypes ?? new Type[0]).Concat(refer.RetryExceptionTypes).ToArray();

            return this;
        }

        private void AssignByValidValue<P>(P original, P refer, Action<P> setter)
        {
            if(!HasValue(original) && HasValue(refer))
                setter(refer);
        }

        private bool HasValue<T>(T value)
        {
            if (object.Equals(value, default(T)) || IsNullableDefault(value))
                return false;
                        
            if (value is string && string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return false;
            }

            if(value is System.Collections.IEnumerable)
            {
                foreach(var item in (value as System.Collections.IEnumerable))
                    return true;

                return false;
            }

            return true;
        }

        private bool IsNullableDefault<T>(T value)
        {
            var type = Nullable.GetUnderlyingType(typeof(T));
            if(type != null)
            {
                return object.Equals(value, type.IsClass ? null : Activator.CreateInstance(type));
            }

            return false;
        }
    }

    /// <summary>
    /// 强类型描述
    /// </summary>
    public abstract class CobServiceTypeDescription : CobServiceDescription
    {
        public abstract string GetUrl(ServiceInfo service, MethodInfo method);
    }

    /// <summary>
    /// 类描述
    /// </summary>
    public class CobServiceClassDescription : CobServiceTypeDescription
    {
        public ConcurrentDictionary<MethodInfo, TypedCobActionDescription> ActionDescriptors { get; private set; } = new ConcurrentDictionary<MethodInfo, TypedCobActionDescription>();

        public CobServiceTypeDescription GetActionOrTypeDesc(MethodInfo action)
        {
            if(ActionDescriptors.ContainsKey(action))
                return ActionDescriptors[action];

            return this;
        }

        public string GetUrlByMethodName(ServiceInfo service, string methodName)
        {
            if (!string.IsNullOrWhiteSpace(methodName))
            {
                var method = ActionDescriptors.Keys.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.InvariantCultureIgnoreCase));

                //按名称没有到本配置，直接使用action拼接返回url
                if (method != null)
                    return this.GetUrl(service, method);
            }

            return base.GetUrl(service, methodName);
        }

        public override string GetUrl(ServiceInfo service, MethodInfo method)
        {
            if (ActionDescriptors.ContainsKey(method))
            {
                return ActionDescriptors[method].GetUrl(service);
            }

            //接用方法名拼接
            return base.GetUrl(service, method.Name);

        }
    }

    /// <summary>
    /// 方法描述
    /// </summary>
    public class TypedCobActionDescription : CobServiceTypeDescription
    {
        public CobServiceDescription Parent { get; internal set; }

        public MethodInfo Method { get; internal set; }
        
        public string GetUrl(ServiceInfo service)
        {
            return !string.IsNullOrWhiteSpace(Path) ? base.GetUrl(service.Address, Path) : Parent.GetUrl(service, Method.Name);
        }

        public override string GetUrl(ServiceInfo service, string action)//忽略action
        {
            return GetUrl(service);
        }

        public override string GetUrl(ServiceInfo service, MethodInfo method)//忽略action
        {
            //todo:ensure method === Method

            return GetUrl(service);
        }

        public override CobServiceDescription Refer(CobServiceDescription refer)
        {
            this.Parent = refer;

            return base.Refer(refer);
        }
    }

    /// <summary>
    /// 根据Type生成服务的描述
    /// </summary>
    public interface ICobServiceDescriptorGenerator
    {
        //TypedCobServiceDescriptor Create<T>() where T : class;

        CobServiceClassDescription Create(Type type);
    }

    public class CobServiceDescriptorGenerator : ICobServiceDescriptorGenerator
    {

        private ConcurrentDictionary<Type, CobServiceClassDescription> _serviceDesc = new ConcurrentDictionary<Type, CobServiceClassDescription>();

        public CobServiceClassDescription Create<T>() where T : class
        {
            return Create(typeof(T));
        }

        public CobServiceClassDescription Create(Type type)
        {
            var desc = _serviceDesc.GetOrAdd(type, t => CreateImpl(t));

            return desc;
        }

        private CobServiceClassDescription CreateImpl(Type targetType)
        {
            var attrs = targetType.GetCustomAttributes(false);

            //CobServiceAttribute
            ParseCobService(attrs, false, out CobServiceClassDescription global);

            foreach(var method in targetType.GetMethods())
            {
                attrs = method.GetCustomAttributes(false);
                if(ParseCobService(attrs, true, out TypedCobActionDescription item) && item != null)
                {
                    //合并方法与全局配置
                    item.Refer(global);
                    item.Method = method;
                    global.ActionDescriptors[method] = item;
                }
            }

            return global;
        }

        private bool ParseCobService<T>(object[] attrs, bool allowMiss, out T desc) where T: CobServiceDescription, new()
        {
            desc = new T();

            //服务配置
            var attr = attrs.FirstOrDefault(a => a is CobServiceAttribute);
            if (attr == null && !allowMiss)
            {
                throw new InvalidOperationException($"missing global CobServiceAttribute for interface/class");
            }

            var hasConfig = false;
            if (attr != null)
            {
                var service = attr as CobServiceAttribute;

                desc.ServiceName = service.ServiceName;
                desc.Path = service.Path;
                desc.Transport = service.Transport;
                desc.Timeout = service.Timeout > 0 ? TimeSpan.FromSeconds(service.Timeout) : TimeSpan.FromSeconds(30);//todo:超时可配置

                hasConfig = true;
            }

            //调用策略
            attr = attrs.FirstOrDefault(a => a is CobStrategyAttribute);
            if(attr != null)
            {
                var strategy = attr as CobStrategyAttribute;

                desc.RetryTimes = strategy.RetryTimes;
                desc.RetryExceptionTypes = strategy.Exceptions;
                desc.FallbackValue = strategy.FallbackValue;

                hasConfig = true;
            }


            return hasConfig;
        }
    }
}
