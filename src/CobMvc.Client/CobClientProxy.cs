using Castle.DynamicProxy;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Common;
using CobMvc.Core.Service;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
                    var ctx = new TypedCobRequestContext() { ServiceName = desc.ServiceName, TargetAddress = service.Address, Url = url, Parameters = parameters, ReturnType = invocation.Method.ReturnType, Method = invocation.Method };//, Timeout = desc.Timeout

                    if (desc.Filters != null)
                    {
                        desc.Filters.ForEach(f => f.OnBeforeRequest(ctx));
                    }

                    return _requestResolver.Get(desc.Transport).DoRequest(ctx, null);


                }, invocation.Method, parameters);
            }
        }
    }


    /// <summary>
    /// 调用服务的辅助包装类
    /// </summary>
    internal class ServiceExecutionEnv : IDisposable
    {
        ILoggerFactory _loggerFactory = null;
        ILogger _logger = null;
        //CobServiceDescription _desc = null;
        ICobServiceSelector _selector = null;

        public ServiceExecutionEnv(ILoggerFactory loggerFactory, ICobServiceSelector selector)/*CobServiceDescription desc, */
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ServiceExecutionEnv>();
            _selector = selector;
        }


        #region 转为同步方法执行

        //public object Execute(Type returnType, CobServiceDescription desc, Func<ServiceInfo, object> action)
        //{
        //    var realType = TaskHelper.GetUnderlyingType(returnType, out bool isTask);

        //    var task = Task.Factory.StartNew(() => {
        //        Func<ServiceInfo, object> method = null;
        //        if (isTask)
        //        {
        //            method = s => TaskHelper.GetResult((Task)action(s));//todo:使用token改为异步等待
        //        }
        //        else
        //        {
        //            method = action;
        //        }

        //        return ExecuteSync(desc, method);
        //    });

        //    if (isTask)
        //        return TaskHelper.ConvertToGeneric(realType, task);

        //    return task.ConfigureAwait(false).GetAwaiter().GetResult();
        //}

        ////同步执行
        //private object ExecuteSync(CobServiceDescription desc, Func<ServiceInfo, object> action)
        //{
        //    var sw = new Stopwatch();
        //    Exception error = null;
        //    object result = null;

        //    //重试
        //    ServiceInfo service = null;
        //    var runTimes = 0;
        //    while (true)
        //    {
        //        service = _selector.GetOne();
        //        if (service != null)
        //        {
        //            runTimes++;
        //            sw.Restart();

        //            try
        //            {
        //                result = action(service);
        //            }
        //            catch (Exception ex)
        //            {
        //                if (desc.RetryTimes > 0 && runTimes < desc.RetryTimes && desc.RetryExceptionTypes != null && desc.RetryExceptionTypes.Length > 0 && desc.RetryExceptionTypes.Contains(ex.GetBaseException().GetType()))
        //                {
        //                    //tofo:等待一段时间后再重试，还要避免出现大量重试请求
        //                    continue;
        //                }

        //                error = ex.GetBaseException();
        //            }

        //            break;
        //        }

        //        _logger.LogError("can not get available service for:{0}", desc.ServiceName);

        //        //todo:无服务可用，降级？
        //        throw new Exception("service select failover");
        //    }

        //    sw.Stop();
        //    SetState(service, sw, error);

        //    return result;
        //}

        //private void SetState(ServiceInfo target, Stopwatch sw, Exception error = null)
        //{
        //    //todo:熔断?
        ////    if (error != null)
        ////    {
        ////        _selector.SetServiceFailed(target);
        ////    }

        //    //todo:设置时间 or 异常
        //    _selector.SetServiceResponseTime(target, sw.Elapsed);
        //}

        #endregion


        #region 都转为Task执行

        /// <summary>
        /// 包状代理，添加重试、超时等机制
        /// </summary>
        /// <param name="returnType"></param>
        /// <param name="desc"></param>
        /// <param name="action"></param>
        /// <param name="method"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object Execute(Type returnType, CobServiceDescription desc, Func<ServiceInfo, Task<object>> action, MethodInfo method = null, Dictionary<string, object> parameters = null)
        {
            Func<ServiceInfo, Task> asyncAction = action;

            //wrap timeout task
            if (desc.Timeout.TotalSeconds > 0)
            {
                asyncAction = s => {
                    var taskTimeout = Task.Delay(desc.Timeout.TotalSeconds > 0 ? desc.Timeout : TimeSpan.FromSeconds(30));//不为空且大于0的超时时间
                    var taskOriginal = action(s);
                    var taskWrapped = Task.WhenAny(taskOriginal, taskTimeout).ContinueWith(t =>
                    {
                        if (t.Result == taskTimeout && taskOriginal.Status < TaskStatus.Running)
                        {
                            throw new TimeoutException();
                        }

                        var result = taskOriginal.Result;

                        if (desc.Filters != null)
                        {
                            desc.Filters.ForEach(f => f.OnAfterResponse(method, result));
                        }

                        return result;
                    });

                    return taskWrapped;
                };
            }

            return ExecuteAsync(returnType, desc, asyncAction, method, parameters);
        }

        private static ConcurrentDictionary<string, object> _scripts = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<Type, ICobFallbackHandler> _fallbackHandlers = new ConcurrentDictionary<Type, ICobFallbackHandler>();
        private object ExecuteAsync(Type returnType, CobServiceDescription desc, Func<ServiceInfo, Task> action, MethodInfo method = null, Dictionary<string, object> parameters = null)
        {
            var retry = new ServiceTaskRetry(_loggerFactory, _selector, desc.RetryTimes, desc.RetryExceptionTypes);

            var taskResult = retry.ExecuteRaw(action).ContinueWith(t => {
                foreach(var item in t.Result)
                {
                    if (item.Service != null)
                    {
                        //set exception,超过阈值后熔断
                        if (item.Exception != null)
                        {
                            _selector.MarkServiceFailed(item.Service, false);
                        }
                        else if (item.Duration.HasValue)
                            _selector.MarkServiceHealthy(item.Service, item.Duration.Value);
                    }
                }

                if (t.Result.All(i => i.Exception != null))
                {
                    var ex = t.Result.First().Exception.GetInnerException();

                    //failover
                    //降级 fallback
                    if (desc.FallbackHandler != null)
                    {
                        _logger.LogInformation("使用缺省值处理类返回");
                        var handler = _fallbackHandlers.GetOrAdd(desc.FallbackHandler, k => Activator.CreateInstance(k) as ICobFallbackHandler);

                        return handler?.GetValue(new CobFallbackHandlerContext { ReturnType = returnType, Exception = ex, ServiceName = desc.ServiceName, Path = desc.Path, Method = method, Parameters = parameters });

                    }
                    else if (!string.IsNullOrWhiteSpace(desc.FallbackValue))
                    {
                        _logger.LogInformation("使用缺省值返回");
                        return _scripts.GetOrAdd(desc.FallbackValue, k => CSharpScript.EvaluateAsync(k).ConfigureAwait(false).GetAwaiter().GetResult());//todo:处理是否为Task
                    }

                    //没有缺省值时，抛出异常
                    throw ex;
                }

                return t.Result.FirstOrDefault(i => i.Exception == null)?.Result;
            });

            var realType = TaskHelper.GetUnderlyingType(returnType, out bool isTask);

            //匹配返回的类型
            if (isTask)
            {
                return TaskHelper.ConvertToGeneric(realType, taskResult);
            }
            else if (realType != null)
            {
                return taskResult.ConfigureAwait(false).GetAwaiter().GetResult();
            }

            return null;//todo:change to default(T) ??
        }

        #endregion

        public void Dispose()
        {
            
        }

        //action重试辅助类
        private class ServiceTaskRetry : IDisposable
        {
            ILogger _logger = null;
            TaskCompletionSource<TaskRetryResult>[] _waiters = null;
            ICobServiceSelector _selector = null;

            int _maxTimes = 1;
            HashSet<Type> _exceptionTypes = null;

            public ServiceTaskRetry(ILoggerFactory loggerFactory, ICobServiceSelector selector, int times, Type[] exceptionTypes)
            {
                _logger = loggerFactory.CreateLogger<ServiceExecutionEnv>();
                _selector = selector;
                _maxTimes = times <= 0 ? 1 : times;
                _exceptionTypes = new HashSet<Type>(exceptionTypes??new Type[0]);

                _waiters = new TaskCompletionSource<TaskRetryResult>[_maxTimes];
                for (var i = 0; i < _maxTimes; i++)
                    _waiters[i] = new TaskCompletionSource<TaskRetryResult>();
            }

            public Task<TaskRetryResult[]> ExecuteRaw(Func<ServiceInfo, Task> action)
            {
                ExecuteImpl(action);

                return Task.WhenAll(_waiters.Select(t => t.Task));
            }

            public Task<object> Execute(Func<ServiceInfo, Task> action)
            {
                return ExecuteRaw(action).ContinueWith(t => {
                    if (t.Result != null)
                    {
                        var result = t.Result.FirstOrDefault(i => i.Exception == null);
                        if (result != null)
                            return result.Result;

                        //exception
                        result = t.Result.FirstOrDefault(i => i.Exception != null);
                        if (result != null)
                            throw result.Exception;
                    }

                    return (object)null;
                });
            }

            private void SetAllCompleted(int index, TaskRetryResult result)
            {
                for (var i = index; i < _maxTimes; i++)
                {
                    _waiters[i].TrySetResult(result);
                }

            }

            private void ExecuteImpl(Func<ServiceInfo, Task> action, int index = 0)
            {
                if (index >= 0 && index < _maxTimes)
                {
                    var service = _selector.GetOne();
                    if (service != null)
                    {
                        var sw = new Stopwatch();
                        sw.Restart();
                        action(service).ContinueWith(t => {
                            TaskRetryResult result = null;
                            if (t.Exception != null)
                            {
                                //_waiters[index].TrySetException(t.Exception);
                                var ex = t.Exception.GetInnerException();//.GetBaseException();
                                result = new TaskRetryResult(service, ex);
                                if (_exceptionTypes.Count == 0 || _exceptionTypes.Contains(ex.GetType()))
                                {
                                    _waiters[index].SetResult(result);

                                    _logger.LogInformation($"执行失败，开始第{index + 1}次重试{service.Name}:{ex.Message}");
                                    ExecuteImpl(action, index + 1);//todo:间隔一段时间重试。。。可配置

                                    return;
                                }

                                //不需要重试，后面统一设置完成状态
                            }
                            else
                            {
                                object value = null;
                                if(t.GetType().IsGenericType)//is Task<T>
                                {
                                    value = t.GetType().GetProperty("Result").GetValue(t);//dynamic?
                                }

                                result = new TaskRetryResult(service, value) { Duration = sw.Elapsed };
                            }

                            //将剩余的设置为完成
                            SetAllCompleted(index, result);
                        });
                    }
                    else
                    {
                        _waiters[index].SetResult(new TaskRetryResult(new Exception("no available service")));//todo:throw cob custom exception
                        ExecuteImpl(action, index + 1);
                    }
                }
            }

            public void Dispose()
            {
                if(_waiters != null)
                {
                    
                }
            }
        }

        private class TaskRetryResult
        {
            public TaskRetryResult(ServiceInfo service, object result)
            {
                Service = service;
                Result = result;
            }

            public TaskRetryResult(Exception exception)
            {
                Exception = exception;
            }

            public TaskRetryResult(ServiceInfo service, Exception exception)
            {
                Service = service;
                Exception = exception;
            }

            public ServiceInfo Service { get; private set; }

            public object Result { get; private set; }

            public Exception Exception { get; private set; }

            public TimeSpan? Duration { get; set; }

            public bool IsSuccess { get => Exception == null; }
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

                    var ctx = new CobRequestContext { ServiceName = _desc.ServiceName, TargetAddress = service.Address, Parameters = parameters, ReturnType = typeof(T), Url = url };//, Timeout = _desc.Timeout

                    return _requestResolver.Get(_desc.Transport).DoRequest(ctx, state);
                });
            }
        }
    }
}
