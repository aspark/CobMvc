using Castle.DynamicProxy;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CobMvc.Client
{
    /// <summary>
    /// 生成客户端的代理工厂
    /// </summary>
    public class CobClientFactory : ICobClientFactory
    {
        ICobRequestResolver _requestResolver = null;
        IServiceRegistration _serviceDiscovery = null;
        ICobServiceDescriptorGenerator _descriptorGenerator = null;
        ILoggerFactory _loggerFactory = null;

        public CobClientFactory(ICobRequestResolver requestResolver, IServiceRegistration serviceDiscovery, ICobServiceDescriptorGenerator descriptorGenerator, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _requestResolver = requestResolver;
            _serviceDiscovery = serviceDiscovery;
            _descriptorGenerator = descriptorGenerator;
        }

        private ConcurrentDictionary<Type, CobClientIInterceptor> _interceptor = new ConcurrentDictionary<Type, CobClientIInterceptor>();
        public T GetProxy<T>() where T : class//CobClientOptions options
        {
            var obj = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(_interceptor.GetOrAdd(typeof(T), type=> {
                var typeDesc = _descriptorGenerator.Create(type);

                return new CobClientIInterceptor(_requestResolver, typeDesc, _serviceDiscovery, _loggerFactory);
            }));

            return obj;
        }

        public ICobClient GetProxy(CobServiceDescription desc)//指定post
        {
            return new CommonCobClient(_requestResolver, _serviceDiscovery, desc, _loggerFactory);
        }

    }

    internal class CobClientIInterceptor : IInterceptor
    {
        TypedCobServiceDescription _typeDesc = null;
        ICobRequestResolver _requestResolver = null;
        ICobServiceSelector _selector = null;
        ILoggerFactory _loggerFactory = null;
        ILogger _logger = null;

        public CobClientIInterceptor(ICobRequestResolver requestResolver, TypedCobServiceDescription typeDesc, IServiceRegistration serviceDiscovery, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CobClientIInterceptor>();
            _typeDesc = typeDesc;
            _requestResolver = requestResolver;//change request by service descriptor
            _selector = new DefaultServiceSelector(serviceDiscovery, _typeDesc.ServiceName, _loggerFactory.CreateLogger<DefaultServiceSelector>());
        }

        public void Intercept(IInvocation invocation)
        {
            var parameters = new Dictionary<string, object>();
            var target = _selector.GetOne();
            if (target != null)
            {
                _logger?.LogDebug("invoke {0}", invocation.Method);

                var url = _typeDesc.GetUrl(target, invocation.Method, out CobServiceDescription desc);

                //设置调用参数
                var names = invocation.Method.GetParameters().Select(p => p.Name).ToArray();
                for (var i = 0; i < invocation.Arguments.Length; i++)
                {
                    parameters[names[i]] = invocation.Arguments[i];
                }

                var ctx = new TypedCobRequestContext() { ServiceName = desc.ServiceName, TargetAddress = target.Address, Url = url, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Timeout = desc.Timeout, Method = invocation.Method };
                //todo:重试，是否需要重选service?
                using (var wrap = new ServiceInfoExecutionEnv(_selector))
                {
                    wrap.Wrap(target, () =>
                    {
                        var ret = _requestResolver.Get(desc.Transport).DoRequest(ctx, null);

                        return invocation.ReturnValue = ret;
                    });

                    return;
                }
            }

            _logger.LogError("can not get available service for:{0}", _typeDesc.ServiceName);

            //todo:无服务可用，降级？
            throw new Exception("service select failover");
        }
    }

    /// <summary>
    /// 调用服务的辅助包装类
    /// </summary>
    internal class ServiceInfoExecutionEnv : IDisposable
    {
        ICobServiceSelector _selector = null;

        public ServiceInfoExecutionEnv(ICobServiceSelector selector)
        {
            _selector = selector;
            var sw = new Stopwatch();
        }

        //public void Wrap(ServiceInfo target, Action action)
        //{
        //    Wrap<object>(target, () => {
        //        action();

        //        return null;
        //    });
        //}

        public T Wrap<T>(ServiceInfo target, Func<T> action)
        {
            var sw = new Stopwatch();

            //todo:重试?
            Exception error = null;
            T result = default(T);

            try
            {
                result = action();
            }
            catch(Exception ex)
            {
                error = ex.GetBaseException();
            }

            void SetFinallyState()
            {
                //todo:熔断?
                if (error != null)
                {
                    _selector.SetServiceFailed(target);

                    //throw error;//???
                }

                //todo:设置时间 or 异常
                _selector.SetServiceResponseTime(target, sw.Elapsed);
            }

            if (result != null)
            {
                if (typeof(Task).IsAssignableFrom(result.GetType()))
                {
                    (result as Task).ContinueWith(_ =>
                    {
                        error = _.Exception?.GetBaseException();
                        SetFinallyState();
                    });
                }
            }
            else
            {
                SetFinallyState();
            }

            return result;
        }

        public void Dispose()
        {
            
        }
    }


    internal class CommonCobClient: ICobClient
    {
        CobServiceDescription _desc = null;
        ICobRequestResolver _requestResolver = null;
        ICobServiceSelector _selector = null;
        //ILogger _logger = null;

        public CommonCobClient(ICobRequestResolver requestResolver, IServiceRegistration serviceDiscovery, CobServiceDescription desc, ILoggerFactory loggerFactory)
        {
            _desc = desc;
            _requestResolver = requestResolver;
            _selector = new DefaultServiceSelector(serviceDiscovery, _desc.ServiceName, loggerFactory?.CreateLogger<DefaultServiceSelector>());
        }

        public T Invoke<T>(string action, Dictionary<string, object> parameters, object state)
        {
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = _desc.GetUrl(target, action);

                var ctx = new CobRequestContext { ServiceName = _desc.ServiceName, TargetAddress = target.Address, Parameters = parameters, ReturnType = typeof(T), Url = url, Timeout = _desc.Timeout };

                using (var wrap = new ServiceInfoExecutionEnv(_selector))
                {
                    return wrap.Wrap(target, () =>
                    {
                        var ret = (T)_requestResolver.Get(_desc.Transport).DoRequest(ctx, state);

                        return ret;
                    });
                }
            }

            return default(T);
        }
    }

}
