using Castle.DynamicProxy;
using CobMvc.Core.Client;
using CobMvc.Core.Common;
using CobMvc.Core.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Client
{

    internal class CobClientProxy : IInterceptor
    {
        CobServiceClassDescription _typeDesc = null;
        ICobRequestResolver _requestResolver = null;
        ICobServiceSelector _selector = null;
        ILoggerFactory _loggerFactory = null;
        ILogger _logger = null;

        public CobClientProxy(ICobRequestResolver requestResolver, CobServiceClassDescription typeDesc, IServiceRegistration serviceDiscovery, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger<CobClientProxy>();
            _typeDesc = typeDesc;
            _requestResolver = requestResolver;//change request by service descriptor
            _selector = new DefaultServiceSelector(serviceDiscovery, _typeDesc.ServiceName, _loggerFactory.CreateLogger<DefaultServiceSelector>());
        }

        public void Intercept(IInvocation invocation)
        {
            var parameters = new Dictionary<string, object>();

            _logger?.LogDebug("invoke {0}", invocation.Method);

            //设置调用参数
            var names = invocation.Method.GetParameters().Select(p => p.Name).ToArray();
            for (var i = 0; i < invocation.Arguments.Length; i++)
            {
                parameters[names[i]] = invocation.Arguments[i];
            }

            using (var env = new ServiceExecutionEnv(_loggerFactory, _selector))
            {
                var desc = _typeDesc.GetActionOrTypeDesc(invocation.Method);

                invocation.ReturnValue = env.Execute(invocation.Method.ReturnType, desc, service => {
                    var url = desc.GetUrl(service, invocation.Method);
                    var ctx = new TypedCobRequestContext() { ServiceName = desc.ServiceName, TargetAddress = service.Address, Url = url, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Timeout = desc.Timeout, Method = invocation.Method };
                    
                    return _requestResolver.Get(desc.Transport).DoRequest(ctx, null);
                });
            }
        }
    }


    /// <summary>
    /// 调用服务的辅助包装类
    /// </summary>
    internal class ServiceExecutionEnv : IDisposable
    {
        ILogger _logger = null;
        //CobServiceDescription _desc = null;
        ICobServiceSelector _selector = null;

        public ServiceExecutionEnv(ILoggerFactory loggerFactory, ICobServiceSelector selector)/*CobServiceDescription desc, */
        {
            _logger = loggerFactory.CreateLogger<ServiceExecutionEnv>();
            _selector = selector;
        }

        public object Execute(Type returnType, CobServiceDescription desc, Func<ServiceInfo, object> action)
        {
            var realType = TaskHelper.GetUnderlyingType(returnType, out bool isTask);

            var task = Task.Factory.StartNew(() => {
                Func<ServiceInfo, object> method = null;
                if (isTask)
                {
                    method = s => TaskHelper.GetResult((Task)action(s));
                }
                else
                {
                    method = action;
                }

                return ExecuteSync(desc, method);
            });

            if (isTask)
                return TaskHelper.ConvertToGeneric(realType, task);

            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        //同下执行
        private object ExecuteSync(CobServiceDescription desc, Func<ServiceInfo, object> action)
        {
            var sw = new Stopwatch();
            Exception error = null;
            object result = null;

            //重试
            ServiceInfo service = null;
            var runTimes = 0;
            while (true)
            {
                service = _selector.GetOne();
                if (service != null)
                {
                    runTimes++;
                    sw.Restart();

                    try
                    {
                        result = action(service);
                    }
                    catch (Exception ex)
                    {
                        if (desc.RetryTimes > 0 && runTimes < desc.RetryTimes && desc.RetryExceptionTypes != null && desc.RetryExceptionTypes.Length > 0 && desc.RetryExceptionTypes.Contains(ex.GetBaseException().GetType()))
                        {
                            //tofo:等待一段时间后再重试，还要避免出现大量重试请求
                            continue;
                        }

                        error = ex.GetBaseException();
                    }

                    break;
                }

                _logger.LogError("can not get available service for:{0}", desc.ServiceName);

                //todo:无服务可用，降级？
                throw new Exception("service select failover");
            }

            sw.Stop();
            SetState(service, sw, error);

            return result;
        }

        private void SetState(ServiceInfo target, Stopwatch sw, Exception error = null)
        {
            //todo:熔断?
            if (error != null)
            {
                _selector.SetServiceFailed(target);
            }

            //todo:设置时间 or 异常
            _selector.SetServiceResponseTime(target, sw.Elapsed);
        }

        public void Dispose()
        {

        }
    }

    //通用的调用代理
    internal class CobCommonClientProxy : ICobClientProxy
    {
        CobServiceDescription _desc = null;
        ICobRequestResolver _requestResolver = null;
        ICobServiceSelector _selector = null;
        //ILogger _logger = null;
        ILoggerFactory _loggerFactory = null;

        public CobCommonClientProxy(ICobRequestResolver requestResolver, IServiceRegistration serviceDiscovery, CobServiceDescription desc, ILoggerFactory loggerFactory)
        {
            _desc = desc;
            _requestResolver = requestResolver;
            _loggerFactory = loggerFactory;
            _selector = new DefaultServiceSelector(serviceDiscovery, _desc.ServiceName, loggerFactory?.CreateLogger<DefaultServiceSelector>());
        }

        public T Invoke<T>(string action, Dictionary<string, object> parameters, object state)
        {
            using (var env = new ServiceExecutionEnv(_loggerFactory, _selector))
            {
                return (T)env.Execute(typeof(T), _desc, service => {
                    var url = _desc.GetUrl(service, action);

                    var ctx = new CobRequestContext { ServiceName = _desc.ServiceName, TargetAddress = service.Address, Parameters = parameters, ReturnType = typeof(T), Url = url, Timeout = _desc.Timeout };

                     return _requestResolver.Get(_desc.Transport).DoRequest(ctx, state);
                });
            }
        }
    }
}
