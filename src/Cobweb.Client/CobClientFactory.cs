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

        public CobClientFactory(ICobRequest request, IServiceRegistration serviceDiscovery)
        {
            _request = request;
            _serviceDiscovery = serviceDiscovery;
        }

        public T GetProxy<T>(CobClientOptions options) where T : class
        {
            var obj = new ProxyGenerator().CreateInterfaceProxyWithoutTarget<T>(new CobClientIInterceptor(_request, options, _serviceDiscovery));

            return obj;
        }

        public ICobClient GetProxy(string serviceName, Dictionary<string, object> parameters, params object[] states)//指定post
        {
            return new CommonCobClient(_request, _serviceDiscovery, serviceName);
        }

    }

    internal class CobClientIInterceptor : IInterceptor
    {
        CobClientOptions _options = null;
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;

        public CobClientIInterceptor(ICobRequest request, CobClientOptions options, IServiceRegistration serviceDiscovery)
        {
            _options = options;
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, options.ServiceName);
        }

        public void Intercept(IInvocation invocation)
        {
            var parameters = new Dictionary<string, object>();
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = target.Address + invocation.Method.Name;

                var ctx = new CobRequestContext() { Url = target.Address, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Method = invocation.Method };
                //todo:重试
                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, null).Result;

                        invocation.ReturnValue = ret;

                        return (object)null;
                    });
                }
                var sw = new Stopwatch();
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
            catch
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
            throw new NotImplementedException();
        }
    }

    internal class CommonCobClient: ICobClient
    {
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;

        public CommonCobClient(ICobRequest request, IServiceRegistration serviceDiscovery, string serviceName)
        {
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, serviceName);
        }

        public Task<T> Invoke<T>(string name, Dictionary<string, object> parameters, params object[] states)
        {
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = target.Address + name;

                var ctx = new CobRequestContext { Parameters = parameters, ReturnType = typeof(T), Url = name };

                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    return wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, states).ContinueWith(t => (T)t.Result);

                        return ret;
                    });
                }
            }

            return Task.FromResult(default(T));
        }
    }

    //public class ad: ICobClient
}
