using CobMvc.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CobMvc.Core.Client
{
    public interface ICobRequest
    {
        /// <summary>
        /// Http/WebSocket etc...
        /// </summary>
        string SupportTransport { get; }//解决原生DI不支持name register

        //object DoRequest(CobRequestContext context, object state);

        Task<object> DoRequest(CobRequestContext context, object state);
    }

    public class CobRequestTransports
    {
        public const string Http = "Http";

        public const string WebSocket = "WebSocket";
    }

    public interface ICobRequestResolver
    {
        ICobRequest Get(string transport);

        void Add(string transport, ICobRequest request);
    }

    internal class DefaultCobRequestResolver : ICobRequestResolver
    {
        public DefaultCobRequestResolver(IServiceProvider serviceProvider)
        {
            foreach (var request in serviceProvider.GetServices<ICobRequest>())
                _dic[request.SupportTransport] = request;
        }

        Dictionary<string, ICobRequest> _dic = new Dictionary<string, ICobRequest>(StringComparer.InvariantCultureIgnoreCase);

        public void Add(string transport, ICobRequest request)
        {
            _dic[transport] = request;
        }

        public ICobRequest Get(string transport)
        {
            if (string.IsNullOrEmpty(transport))
                transport = CobRequestTransports.Http;

            if (_dic.ContainsKey(transport))
                return _dic[transport];

            //return null;
            throw new NotSupportedException(transport);
        }
    }

    public class CobRequestContext
    {
        public string ServiceName { get; set; }

        public string TargetAddress { get; set; }

        /// <summary>
        /// 调用路径（暂无Query）
        /// </summary>
        public string Url { get; set; }

        public Dictionary<string, object> Parameters { get; set; }

        //public object Body { get; set; }

        /// <summary>
        /// 如果为void非为null
        /// </summary>
        public Type ReturnType { get; set; }

        //public TimeSpan Timeout { get; set; }
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
        public Task<object> DoRequest(CobRequestContext context, object state)
        {
            return MatchRealType(context, realType => DoRequest(context, realType, state));
        }

        public abstract string SupportTransport { get; }

        protected abstract Task<object> DoRequest(CobRequestContext context, Type realType, object state);


        protected internal Task<object> MatchRealType(CobRequestContext context, Func<Type, Task<object>> action)
        {
            var realReturnType = TaskHelper.GetUnderlyingType(context.ReturnType, out bool isTask);//去掉task/void等泛型

            return action(realReturnType);
        }
        
        public virtual string GetDebugInfo()
        {
            return string.Empty;
        }
    }
}
