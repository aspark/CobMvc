using Castle.DynamicProxy;
using Cobweb.Core;
using Cobweb.Core.Client;
using Cobweb.Core.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cobweb.Client
{
    public class CobClientFactory : ICobClientFactory
    {
        ICobRequest _request = null;
        IServiceRegistration _serviceDiscovery = null;
        ICobServiceDescriptorGenerator _descriptorGenerator = null;

        public CobClientFactory(ICobRequest request, IServiceRegistration serviceDiscovery, ICobServiceDescriptorGenerator descriptorGenerator)
        {
            _request = request;
            _serviceDiscovery = serviceDiscovery;
            _descriptorGenerator = descriptorGenerator;
        }

        public T GetProxy<T>() where T : class//CobClientOptions options
        {
            var desc = new CobServiceDescriptorGenerator().Create<T>();//todo;DI

            var obj = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(new CobClientIInterceptor(_request, desc, _serviceDiscovery, _descriptorGenerator));

            return obj;
        }

        public ICobClient GetProxy(CobServiceDescriptor desc)//指定post
        {
            return new CommonCobClient(_request, _serviceDiscovery, desc);
        }

    }

    internal class CobClientIInterceptor : IInterceptor
    {
        CobServiceDescriptor _descGlobal = null;
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;
        ICobServiceDescriptorGenerator _descriptorGenerator = null;

        public CobClientIInterceptor(ICobRequest request, CobServiceDescriptor desc, IServiceRegistration serviceDiscovery, ICobServiceDescriptorGenerator descriptorGenerator)
        {
            _descGlobal = desc;
            _request = request;
            _descriptorGenerator = descriptorGenerator;
            _selector = new DefaultServiceSelector(serviceDiscovery, _descGlobal.ServiceName);
        }

        public void Intercept(IInvocation invocation)
        {
            var parameters = new Dictionary<string, object>();
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = _descriptorGenerator.Create(invocation.TargetType).GetUrl(target, invocation.Method);

                //设置调用参数
                var names = invocation.Method.GetParameters().Select(p => p.Name).ToArray();
                for (var i = 0; i < invocation.Arguments.Length; i++)
                {
                    parameters[names[i]] = invocation.Arguments[i];
                }

                var ctx = new TypedCobRequestContext() { Url = url, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Method = invocation.Method };
                //todo:重试
                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, null).Result;

                        invocation.ReturnValue = ret;

                        return (object)null;
                    });

                    return;
                }
            }

            //todo:无服务可用，降级？
            throw new Exception("failover");
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

            //todo:重试
            try
            {
                return action();
            }
            catch(Exception ex)
            {
                //todo:熔断
                _selector.SetServiceFailed(target);
            }
            finally
            {
                //todo:设置时间 or 异常
                _selector.SetServiceResponseTime(target, sw.Elapsed);
            }

            return default(T);
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

        public CommonCobClient(ICobRequest request, IServiceRegistration serviceDiscovery, CobServiceDescriptor desc)
        {
            _desc = desc;
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, _desc.ServiceName);
        }

        public Task<T> Invoke<T>(string action, Dictionary<string, object> parameters, object state)
        {
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = _desc.GetUrl(target, action);

                var ctx = new CobRequestContext { Parameters = parameters, ReturnType = typeof(T), Url = url };

                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    return wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, state).ContinueWith(t => (T)t.Result);

                        return ret;
                    });
                }
            }

            return Task.FromResult(default(T));
        }
    }

    //public class ad: ICobClient
}
