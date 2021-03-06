﻿using CobMvc.Core.Common;
using CobMvc.Core.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
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
    public abstract class CobServiceDescription// : ICloneable
    {
        public CobServiceDescription()
        {
            Filters = new ICobRequestFilter[0];
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        internal void EnsureValue()
        {
            if(ResolveServiceName == EnumResolveServiceName.NotSet)
                ResolveServiceName = EnumResolveServiceName.ResolveServiceName;

            if(string.IsNullOrWhiteSpace(Transport))
                Transport = CobRequestTransports.Http;

            //Formatter = "";

            if (RetryTimes <= 0)
                RetryTimes = 3;
        }

        /// <summary>
        /// 服务名
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 将服务名替换为服务发现中的Host，默认当作true。如需使用sidecar等代理模式请设置为false
        /// </summary>
        public EnumResolveServiceName ResolveServiceName { get; set; }

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
        /// 失败回退时的处理类
        /// </summary>
        public Type FallbackHandler { get; set; }

        /// <summary>
        /// 重试异常
        /// </summary>
        public Type[] RetryExceptionTypes { get; set; }

        public ICobRequestFilter[] Filters { get; set; }

        private static ConcurrentDictionary<string, string> _serviceNameAddr = new ConcurrentDictionary<string, string>();
        protected string ResolveAddress(ServiceInfo service)
        {
            if(ResolveServiceName == EnumResolveServiceName.KeepServiceName)
            {
                if(!string.Equals(Transport, CobRequestTransports.Http, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("ResolveServiceName noly support http/https transport");
                }

                return _serviceNameAddr.GetOrAdd(service.Name, k => {
                    var builder = new UriBuilder(service.Address);

                    //去除host port等信息
                    builder.Host = service.Name;

                    if(string.Equals(builder.Scheme, "https", StringComparison.InvariantCultureIgnoreCase))
                        builder.Port = 443;
                    else
                        builder.Port = 80;

                    return builder.Uri.ToString();
                });
            }

            return service.Address;
        }

        /// <summary>
        /// 根据服务获取调用的地址
        /// </summary>
        /// <param name="service"></param>
        /// <param name="action">方法名</param>
        /// <returns></returns>
        public virtual string GetUrl(ServiceInfo service, string action)
        {
            return GetUrl(ResolveAddress(service), Path, action);//?.ToString() ?? ""
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
        internal protected virtual CobServiceDescription Refer(CobServiceDescription refer)
        {
            AssignByValidValue(this.ServiceName, refer.ServiceName, v => ServiceName = v);

            if (HasValue(Path) && HasValue(refer.Path))
                Path = UriHelper.Combine(refer.Path, Path);// AssignByValidValue(this.Path, refer.Path, v => );

            AssignByValidValue(this.RetryTimes, refer.RetryTimes, v => RetryTimes = v);
            AssignByValidValue(this.Timeout, refer.Timeout, v => Timeout = v);
            AssignByValidValue(this.Transport, refer.Transport, v => Transport = v);
            AssignByValidValue(this.FallbackValue, refer.FallbackValue, v => FallbackValue = v);
            AssignByValidValue(this.FallbackHandler, refer.FallbackHandler, v => FallbackHandler = v);

            if (HasValue(refer.RetryExceptionTypes))
                this.RetryExceptionTypes = (this.RetryExceptionTypes ?? new Type[0]).Concat(refer.RetryExceptionTypes).ToArray();

            //AssignByValidValue(this.Formatter, refer.Formatter, v => Formatter = v);

            AssignByValidValue(this.ResolveServiceName, refer.ResolveServiceName, v => ResolveServiceName = v);


            if (HasValue(refer.Filters))
                this.Filters = refer.Filters.Concat(this.Filters ?? new ICobRequestFilter[0]).ToArray();//refer的filter在方法的前面

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
        public ConcurrentDictionary<MethodInfo, CobServiceActionDescription> ActionDescriptors { get; private set; } = new ConcurrentDictionary<MethodInfo, CobServiceActionDescription>();

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
    public class CobServiceActionDescription : CobServiceTypeDescription
    {
        public CobServiceDescription Parent { get; internal set; }

        public MethodInfo Method { get; internal set; }
        
        public string GetUrl(ServiceInfo service)
        {
            var url = !string.IsNullOrWhiteSpace(Path) ? base.GetUrl(ResolveAddress(service), Path) : Parent.GetUrl(service, Method.Name);

            return url;
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

        internal protected override CobServiceDescription Refer(CobServiceDescription refer)
        {
            this.Parent = refer;

            return base.Refer(refer);
        }
    }

    /// <summary>
    /// 根据Type生成服务的描述
    /// </summary>
    public interface ICobServiceDescriptionGenerator
    {
        //TypedCobServiceDescriptor Create<T>() where T : class;

        CobServiceClassDescription Create(Type type);
    }

    public class CobServiceDescriptionGenerator : ICobServiceDescriptionGenerator
    {
        CobMvcRequestOptions _requestOptions;
        IConfiguration _configuration;

        public CobServiceDescriptionGenerator(IOptions<CobMvcRequestOptions> requestOptions, IConfiguration configuration)
        {
            _requestOptions = requestOptions.Value;
            _configuration = configuration;
        }

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
            ParseCobService(attrs, false, true, out CobServiceClassDescription global);
            global.EnsureValue();

            foreach (var method in targetType.GetMethods())
            {
                attrs = method.GetCustomAttributes(false);
                if(ParseCobService(attrs, true, false, out CobServiceActionDescription item) && item != null)
                {
                    //合并方法与全局配置
                    item.Refer(global);
                    item.Method = method;
                    global.ActionDescriptors[method] = item;
                }
            }

            return global;
        }

        private bool ParseCobService<T>(object[] attrs, bool allowMiss, bool allowConfig, out T desc) where T: CobServiceDescription, new()
        {
            desc = new T();

            object attr = null;
            CobServiceAttribute configService = null;

            //服务配置
            if (allowConfig)
            {
                attr = attrs.FirstOrDefault(a => a is CobServiceFromConfigAttribute);
                if(attr != null)
                {
                    var config = attr as CobServiceFromConfigAttribute;

                    if(_configuration == null || !_configuration.GetSection(config.SectionKey).Exists())
                    {
                        throw new Exception($"configuration is null or {config.SectionKey} section is not exists");
                    }

                    configService = _configuration.GetSection(config.SectionKey).Get<CobServiceAttribute>();
                }

            }

            //有配置后，忽略attr中的配置
            //todo:还是用config的覆盖attr
            attr = configService ?? attrs.FirstOrDefault(a => a is CobServiceAttribute);
            if (attr == null && !allowMiss)
            {
                throw new InvalidOperationException($"missing global CobServiceAttribute for interface/class");
            }

            var hasConfig = false;
            if (attr != null)
            {
                var service = attr as CobServiceAttribute;

                desc.ServiceName = service.ServiceName;
                desc.ResolveServiceName = service.ResolveServiceName;
                desc.Path = service.Path;
                desc.Transport = service.Transport;
                desc.Timeout = service.Timeout > 0 ? TimeSpan.FromSeconds(service.Timeout) : TimeSpan.FromSeconds(_requestOptions.DefaultTimeout);//超时

                hasConfig = true;
            }

            //调用策略
            attr = attrs.FirstOrDefault(a => a is CobRetryStrategyAttribute);
            if(attr != null)
            {
                var strategy = attr as CobRetryStrategyAttribute;

                desc.RetryTimes = strategy.Count;
                desc.RetryExceptionTypes = strategy.Exceptions;
                desc.FallbackValue = strategy.FallbackValue;
                desc.FallbackHandler = strategy.FallbackHandler;

                hasConfig = true;
            }

            //过滤器
            var filters = attrs.Where(a => a is ICobRequestFilter);
            if(filters.Any())
            {
                desc.Filters = filters.Cast<ICobRequestFilter>().OrderBy(f => f.Order).ToArray();

                hasConfig = true;
            }


            return hasConfig;
        }
    }
}
