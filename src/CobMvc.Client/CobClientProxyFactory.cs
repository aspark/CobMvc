using Castle.DynamicProxy;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        ICobServiceDescriptionGenerator _descriptorGenerator = null;
        ILoggerFactory _loggerFactory = null;
        Lazy<ProxyGenerator> _proxyGenerator = null;
        ICobMvcContextAccessor _contextAccessor = null;
        IOptions<CobMvcRequestOptions> _requestOptions;

        public CobClientProxyFactory(ICobRequestResolver requestResolver, IServiceRegistration serviceDiscovery, ICobServiceDescriptionGenerator descriptorGenerator,
            ILoggerFactory loggerFactory, ICobMvcContextAccessor contextAccessor, IOptions<CobMvcRequestOptions> requestOptions)
        {
            _proxyGenerator = new Lazy<ProxyGenerator>(()=> new ProxyGenerator(), false);
            _loggerFactory = loggerFactory;
            _requestResolver = requestResolver;
            _serviceDiscovery = serviceDiscovery;
            _descriptorGenerator = descriptorGenerator;
            _contextAccessor = contextAccessor;
            _requestOptions = requestOptions;
        }

        private ConcurrentDictionary<Type, CobClientProxy> _interceptor = new ConcurrentDictionary<Type, CobClientProxy>();
        public T GetProxy<T>() where T : class//CobClientOptions options
        {
            var obj = _proxyGenerator.Value.CreateInterfaceProxyWithoutTarget<T>(_interceptor.GetOrAdd(typeof(T), type=> {
                var typeDesc = _descriptorGenerator.Create(type);

                return new CobClientProxy(_requestResolver, typeDesc, _serviceDiscovery, _loggerFactory, _contextAccessor, _requestOptions);
            }));

            return obj;
        }

        public ICobClientProxy GetProxy(CobServiceDescription desc)//指定post
        {
            return new CobCommonClientProxy(_requestResolver, _serviceDiscovery, desc, _loggerFactory, _requestOptions);
        }

    }
}
