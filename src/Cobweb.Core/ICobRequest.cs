using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Cobweb.Core
{
    public interface ICobRequest
    {
        object DoRequest(CobRequestContext context, object state);
    }

    public class CobRequestContext
    {
        public string Url { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        //public object Body { get; set; }

        /// <summary>
        /// 如果为void非为null
        /// </summary>
        public Type ReturnType { get; set; }
    }

    /// <summary>
    /// 使用接口生成的调用上下文
    /// </summary>
    public class TypedCobRequestContext : CobRequestContext
    {
        /// <summary>
        /// 
        /// </summary>
        public MethodInfo Method { get; set; }
    }

    public abstract class CobRequestBase : ICobRequest
    {
        public abstract object DoRequest(CobRequestContext context, object state);


        internal protected object MatchReturnType(Type returnType, Func<Type, Task<object>> converter)
        {
            var isTask = false;
            var realReturnType = returnType;//去掉task/void等泛型
            if (typeof(Task).IsAssignableFrom(realReturnType))
            {

                isTask = true;
                if (realReturnType.IsGenericType)
                    realReturnType = realReturnType.GetGenericArguments().First();
                else
                    realReturnType = null;//无返回值
            }
            else if (realReturnType == typeof(void))
            {
                realReturnType = null;
            }

            if (isTask)
            {
                if (realReturnType == null)//Task
                {
                    return converter(realReturnType);
                }
                else//Task<T>
                {
                    return CreateGenericTask(realReturnType, converter(realReturnType));
                }
            }
            else if (realReturnType != null)
            {
                return converter(realReturnType).Result;
            }

            return null;
        }

        private Task CreateGenericTask(Type type, Task<object> obj)
        {
            var gt = typeof(TaskCompletionSource<>).MakeGenericType(type);
            var tcs = Activator.CreateInstance(gt);
            obj.ContinueWith(t => {
                gt.GetMethod(nameof(TaskCompletionSource<int>.TrySetResult)).Invoke(tcs, new[] { t.Result });
            });

            return gt.GetProperty(nameof(TaskCompletionSource<int>.Task)).GetValue(tcs) as Task;
        }
    }
}
