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
    public class CobClientFactory : ICobClientFactory
    {
        ICobRequest _request = null;
        IServiceRegistration _serviceDiscovery = null;
        ICobServiceDescriptorGenerator _descriptorGenerator = null;
        ILoggerFactory _loggerFactory = null;

        public CobClientFactory(ICobRequest request, IServiceRegistration serviceDiscovery, ICobServiceDescriptorGenerator descriptorGenerator, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _request = request;
            _serviceDiscovery = serviceDiscovery;
            _descriptorGenerator = descriptorGenerator;
        }

        private ConcurrentDictionary<Type, CobClientIInterceptor> _interceptor = new ConcurrentDictionary<Type, CobClientIInterceptor>();
        public T GetProxy<T>() where T : class//CobClientOptions options
        {
            var obj = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(_interceptor.GetOrAdd(typeof(T), type=> {
                var desc = _descriptorGenerator.Create(type);

                return new CobClientIInterceptor(_request, desc, _serviceDiscovery, _loggerFactory);
            }));

            return obj;
        }

        public ICobClient GetProxy(CobServiceDescriptor desc)//指定post
        {
            return new CommonCobClient(_request, _serviceDiscovery, desc, _loggerFactory);
        }

    }

    internal class CobClientIInterceptor : IInterceptor
    {
        CobServiceDescriptor _desc = null;
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;
        ILoggerFactory _loggerFactory = null;
        ILogger _logger = null;

        public CobClientIInterceptor(ICobRequest request, CobServiceDescriptor desc, IServiceRegistration serviceDiscovery, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CobClientIInterceptor>();
            _desc = desc;
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, _desc.ServiceName, _loggerFactory.CreateLogger<DefaultServiceSelector>());
        }

        public void Intercept(IInvocation invocation)
        {
            var parameters = new Dictionary<string, object>();
            var target = _selector.GetOne();
            if (target != null)
            {
                _logger?.LogDebug("invoke {0}", invocation.Method);

                var url = _desc.GetUrl(target, invocation.Method);

                //设置调用参数
                var names = invocation.Method.GetParameters().Select(p => p.Name).ToArray();
                for (var i = 0; i < invocation.Arguments.Length; i++)
                {
                    parameters[names[i]] = invocation.Arguments[i];
                }

                var ctx = new TypedCobRequestContext() { ServiceName = _desc.ServiceName, TargetAddress = target.Address, Url = url, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Method = invocation.Method };
                //todo:重试，是否需要重选service?
                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, null);

                        return invocation.ReturnValue = ret;
                    });

                    return;
                }
            }

            _logger.LogError("can not get available service for:{0}", _desc.ServiceName);

            //todo:无服务可用，降级？
            throw new Exception("service select failover");
        }
    }

    internal class ServiceInfoExecution : IDisposable
    {
        ICobServiceSelector _selector = null;

        public ServiceInfoExecution(ICobServiceSelector selector)
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
        CobServiceDescriptor _desc = null;
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;
        //ILogger _logger = null;

        public CommonCobClient(ICobRequest request, IServiceRegistration serviceDiscovery, CobServiceDescriptor desc, ILoggerFactory loggerFactory)
        {
            _desc = desc;
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, _desc.ServiceName, loggerFactory?.CreateLogger<DefaultServiceSelector>());
        }

        public T Invoke<T>(string action, Dictionary<string, object> parameters, object state)
        {
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = _desc.GetUrl(target, action);

                var ctx = new CobRequestContext { ServiceName = _desc.ServiceName, TargetAddress = target.Address, Parameters = parameters, ReturnType = typeof(T), Url = url };

                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    return wrap.Wrap(target, () =>
                    {
                        var ret = (T)_request.DoRequest(ctx, state);

                        return ret;
                    });
                }
            }

            return default(T);
        }
    }

}
