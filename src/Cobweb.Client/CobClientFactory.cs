using Castle.DynamicProxy;
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
    public class CobClientFactory
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

        public T Invoke<T>(string serviceName, Dictionary<string, object> parameters, params object[] states)//指定post
        {
            var client = new CobCommonClient(_request, _serviceDiscovery, serviceName);

            return (T)(client.DoRequest<T>(serviceName, parameters, states).Result);
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
                var url = target.Host + invocation.Method.Name;

                var ctx = new CobRequestContext() { Url = target.Host, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Method = invocation.Method };
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

                //todo:重试
                try
                {

                    sw.Restart();
                    var ret = _request.DoRequest(ctx, null).Result;
                    sw.Stop();

                    invocation.ReturnValue = ret;

                }
                catch {
                    //todo:熔断
                    _selector.IncreaseFailedCount(target);
                }
                finally
                {
                    //todo:设置时间 or 异常
                    _selector.SetResponseTime(target, sw.Elapsed);
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
            catch
            {
                //todo:熔断
                _selector.IncreaseFailedCount(target);
            }
            finally
            {
                //todo:设置时间 or 异常
                _selector.SetResponseTime(target, sw.Elapsed);
            }

            return default(T);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    internal class CobCommonClient
    {
        ICobRequest _request = null;
        ICobServiceSelector _selector = null;

        public CobCommonClient(ICobRequest request, IServiceRegistration serviceDiscovery, string serviceName)
        {
            _request = request;
            _selector = new DefaultServiceSelector(serviceDiscovery, serviceName);
        }

        public Task<object> DoRequest<T>(string name, Dictionary<string, object> parameters, params object[] states)
        {
            var target = _selector.GetOne();
            if (target != null)
            {
                var url = target.Host + name;

                var ctx = new CobRequestContext { Parameters = parameters, ReturnType = typeof(T), Url = name };

                using (var wrap = new ServiceInfoExecution(_selector))
                {
                    return wrap.Wrap(target, () =>
                    {
                        var ret = _request.DoRequest(ctx, states);

                        return ret;
                    });
                }
            }

            return Task.FromResult<object>(default(T));
        }
    }

    //public class ad: ICobClient
}
