using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Shouldly;
using CobMvc.Client;
using Moq;
using CobMvc.Core.Client;
using CobMvc.Core.InMemory;
using Microsoft.Extensions.Options;
using CobMvc.Core;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using CobMvc.Core.Common;

namespace CobMvc.Test
{
    public class CobClientProxyTest
    {
        [Fact]
        public void TestProxyFactory()
        {
            CobRequestContext context = null;
            object state = null;

            var request = new Mock<ICobRequest>();
            request.Setup(m => m.DoRequest(It.IsAny<CobRequestContext>(), It.IsAny<object>())).Callback<CobRequestContext, object>((ctx, s)=> {
                context = ctx;
                state = s;
            }).Returns<CobRequestContext, object>((ctx, s) => {
                if (ctx.ReturnType != null)
                {
                    var type = TaskHelper.GetUnderlyingType(ctx.ReturnType, out _);
                    if(type != null)
                    {
                        object value = null;
                        if (type.IsValueType)
                            value = Activator.CreateInstance(type);

                        return Task.FromResult(value);
                    }
                }

                return Task.FromResult<object>(null);
            });

            var requestResovler = new Mock<ICobRequestResolver>();
            requestResovler.Setup(m => m.Get(It.IsAny<string>())).Returns(request.Object);

            var contextAccessor = new CobMvcContextAccessor();

            var serviceRegistration = new InMemoryServiceRegistration();
            serviceRegistration.Register(new Core.Service.ServiceInfo { ID = "a", Name = nameof(ContractA), Address="http://cobmvc.test", Status = Core.Service.ServiceInfoStatus.Healthy });
            var requestOptions = Options.Create(new CobMvcRequestOptions());

            var factory = new CobClientProxyFactory(
                requestResovler.Object, 
                serviceRegistration, 
                new CobServiceDescriptionGenerator(requestOptions), 
                new LoggerFactory(),
                contextAccessor,
                requestOptions);


            var proxy = factory.GetProxy<ContractA>();

            object result = proxy.GetScore(1);
            context.ShouldNotBeNull();
            //result.ShouldBe(0);
            context.Url.ShouldBe("http://ContractA/sys/score", StringCompareShould.IgnoreCase);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            context.Extensions.ShouldContainKey("author");
            context.Extensions["author"].ShouldBe("admin", StringCompareShould.IgnoreCase);
            context.Parameters.ShouldContainKey("id");
            context.Parameters["id"].ShouldBe(1);

            result = proxy.GetName("aspark").GetAwaiter().GetResult();
            context.ShouldNotBeNull();
            context.Url.ShouldBe("http://ContractA/api/v1/name", StringCompareShould.IgnoreCase);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            context.Parameters.ShouldContainKey("id");
            context.Parameters["id"].ShouldBe("aspark");

            result = proxy.SaveChanged(new Dto { }).GetAwaiter().GetResult();
            context.ShouldNotBeNull();
            context.Url.ShouldBe("http://ContractA/api/v1/SaveChanged", StringCompareShould.IgnoreCase);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            context.Extensions.ShouldContainKey("author");
            context.Extensions["author"].ShouldBe("aspark", StringCompareShould.IgnoreCase);
            context.Parameters.ShouldContainKey("dto");
            context.Parameters["dto"].ShouldBeAssignableTo<Dto>();
        }

        [CobService(nameof(ContractA), Path = "/api/v1", ResolveServiceName = EnumResolveServiceName.KeepServiceName)]
        [CobRetryStrategy(Count = 2)]
        [RequestFilter(Name = "admin")]
        public interface ContractA
        {
            [CobService(Path = "/sys/score")]
            int GetScore(int id);

            [CobService(Path = "name")]
            [CobRetryStrategy(FallbackValue = "not exists")]
            Task<string> GetName(string id);

            [CobService(Timeout = 3, Transport = CobRequestTransports.WebSocket)]
            [RequestFilter(Name = "aspark")]
            Task<bool> SaveChanged(Dto dto);
        }

        public class ServiceA : ContractA
        {
            public Task<string> GetName(string id)
            {
                //return Task.FromResult("@" + id);
                throw new NotImplementedException();
            }

            public int GetScore(int id)
            {
                return id + 1;
            }

            public Task<bool> SaveChanged(Dto dto)
            {
                return Task.FromResult(true);
            }
        }

        public class Dto
        { }

        public class RequestFilter : CobRequestFilterAttribute
        {
            public string Name { get; set; }

            public override void OnBeforeRequest(CobRequestContext context)
            {
                //base.OnBeforeRequest(context);
                context.Extensions["author"] = Name;
            }
        }
    }
}
