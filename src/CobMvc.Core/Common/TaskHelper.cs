using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Core.Common
{
    public class TaskHelper
    {
        public static Task<T> ConvertToGeneric<T>(Task<object> obj)
        {
            return (Task<T>)ConvertToGeneric(typeof(T), obj);
        }

        public static Task ConvertToGeneric(Type innerType, Task<object> obj)
        {
            if (innerType == null)
                return obj;

            var gt = typeof(TaskCompletionSource<>).MakeGenericType(innerType);
            var tcs = Activator.CreateInstance(gt);
            void setTaskException(Exception ex)
            {
                gt.GetMethod(nameof(TaskCompletionSource<int>.TrySetException), new[] { typeof(Exception) }).Invoke(tcs, new[] { ex });
            }
            obj.ContinueWith(t => {
                try
                {
                    if (t.Exception == null)
                    {
                        gt.GetMethod(nameof(TaskCompletionSource<int>.TrySetResult)).Invoke(tcs, new[] { t.Result });
                    }
                    else
                    {
                        setTaskException(t.Exception.GetBaseException());
                    }
                }
                catch (Exception ex)
                {
                    setTaskException(ex.GetBaseException());
                }
            });

            return gt.GetProperty(nameof(TaskCompletionSource<int>.Task)).GetValue(tcs) as Task;
        }

        public static Type GetUnderlyingType(Type type, out bool isTask)
        {
            isTask = false;
            var realType = type;//去掉task/void等泛型
            if (typeof(Task).IsAssignableFrom(realType))
            {
                isTask = true;
                if (realType.IsGenericType)
                    realType = realType.GetGenericArguments().First();
                else
                    realType = null;//无返回值
            }
            else if (realType == typeof(void))
            {
                realType = null;
            }

            return realType;
        }

        public static object GetResult(Task task)
        {
            //var result = ((Task)task).ConfigureAwait(false).GetAwaiter().GetResult();

            var type = task.GetType();

            var result = type.GetMethod("ConfigureAwait").Invoke(task, new object[] { false });
            result = result.GetType().GetMethod("GetAwaiter").Invoke(result, null);
            result = result.GetType().GetMethod("GetResult").Invoke(result, null);

            if (type.IsGenericType)
                return result;

            return null;
        }
    }
}
