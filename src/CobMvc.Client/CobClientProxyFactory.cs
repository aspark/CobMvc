using Castle.DynamicProxy;
using CobMvc.Core.Client;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace CobMvc.Client
{
    /// <summary>
    /// 生成客户端的代理工厂
    /// </summary>
    internal class CobClientProxyFactory : ICobClientFactory
    {
        ICobRequestResolver _requestResolver = null;
        IServiceRegistration _serviceDiscovery = null;
        ICobServiceDescriptorGenerator _descriptorGenerator = null;
        ILoggerFactory _loggerFactory = null;

        public CobClientProxyFactory(ICobRequestResolver requestResolver, IServiceRegistration serviceDiscovery, ICobServiceDescriptorGenerator descriptorGenerator, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _requestResolver = requestResolver;
            _serviceDiscovery = serviceDiscovery;
            _descriptorGenerator = descriptorGenerator;
        }

        private ConcurrentDictionary<Type, CobClientProxy> _interceptor = new ConcurrentDictionary<Type, CobClientProxy>();
        public T GetProxy<T>() where T : class//CobClientOptions options
        {
            var obj = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(_interceptor.GetOrAdd(typeof(T), type=> {
                var typeDesc = _descriptorGenerator.Create(type);

                return new CobClientProxy(_requestResolver, typeDesc, _serviceDiscovery, _loggerFactory);
            }));

            return obj;
        }

        public ICobClientProxy GetProxy(CobServiceDescription desc)//指定post
        {
            return new CobCommonClientProxy(_requestResolver, _serviceDiscovery, desc, _loggerFactory);
        }

    }
}
