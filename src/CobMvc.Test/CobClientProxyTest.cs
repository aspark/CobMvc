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
using System.Linq;

namespace CobMvc.Test
{
    public class CobClientProxyTest
    {
        class ProxyFactoryBag
        {
            public ICobClientFactory Factory { get; set; }

            public CobRequestContext Context { get; set; }

            public object State { get; set; }

            public int InvokeCount { get; set; }

            public void Reset()
            {
                Context = null;
                State = null;
                InvokeCount = 0;
            }
        }

        private ProxyFactoryBag CreateProxyFactory()
        {
            ProxyFactoryBag result = new ProxyFactoryBag();

            var request = new Mock<ICobRequest>();
            request.Setup(m => m.DoRequest(It.IsAny<CobRequestContext>(), It.IsAny<object>())).Callback<CobRequestContext, object>((ctx, s) => {
                result.Context = ctx;
                result.State = s;
                result.InvokeCount++;
            }).Returns<CobRequestContext, object>((ctx, s) => {
                var typed = ctx as TypedCobRequestContext;
                var service = new ServiceA();
                object value = null;

                switch (typed.Method.Name)
                {
                    case nameof(ContractA.GetName):
                        value = service.GetName(ctx.Parameters["id"].ToString()).Result;
                        break;
                    case nameof(ContractA.GetScore):
                        value = service.GetScore((int)ctx.Parameters["id"]);
                        break;
                    case nameof(ContractA.SaveChanged):
                        value = service.SaveChanged((Dto)ctx.Parameters["dto"]).Result;
                        break;
                }

                return Task.FromResult(value);
            });

            var requestResovler = new Mock<ICobRequestResolver>();
            requestResovler.Setup(m => m.Get(It.IsAny<string>())).Returns(request.Object);

            var contextAccessor = new CobMvcContextAccessor();

            var serviceRegistration = new InMemoryServiceRegistration();
            serviceRegistration.Register(new Core.Service.ServiceInfo { ID = "a", Name = nameof(ContractA), Address = "http://cobmvc.test", Status = Core.Service.ServiceInfoStatus.Healthy });
            var requestOptions = Options.Create(new CobMvcRequestOptions());

            result.Factory = new CobClientProxyFactory(
                requestResovler.Object,
                serviceRegistration,
                new CobServiceDescriptionGenerator(requestOptions),
                new LoggerFactory(),
                contextAccessor,
                requestOptions);

            return result;
        }

        [Fact]
        public void TestProxyFactory()
        {
            var bag = CreateProxyFactory();
            //完成构造factory
            var proxy = bag.Factory.GetProxy<ContractA>();

            bag.Reset();
            object result = proxy.GetScore(1);
            bag.Context.ShouldNotBeNull();
            bag.InvokeCount.ShouldBe(1);
            result.ShouldBe(2);
            bag.Context.Url.ShouldBe("http://ContractA/sys/score", StringCompareShould.IgnoreCase);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            bag.Context.Extensions.ShouldContainKey("author");
            bag.Context.Extensions["author"].ShouldBe("admin", StringCompareShould.IgnoreCase);
            bag.Context.Parameters.ShouldContainKey("id");
            bag.Context.Parameters["id"].ShouldBe(1);

            bag.Reset();
            result = proxy.SaveChanged(new Dto { Opt = 1 }).GetAwaiter().GetResult();
            bag.Context.ShouldNotBeNull();
            result.ShouldBe(true);
            bag.Context.Url.ShouldBe("http://ContractA/api/v1/SaveChanged", StringCompareShould.IgnoreCase);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            bag.Context.Extensions.ShouldContainKey("author");
            bag.Context.Extensions["author"].ShouldBe("aspark", StringCompareShould.IgnoreCase);
            bag.Context.Parameters.ShouldContainKey("dto");
            bag.Context.Parameters["dto"].ShouldBeAssignableTo<Dto>();
            (bag.Context.Parameters["dto"] as Dto).Opt.ShouldBe(1);

            //exception的放置到后面，因为这个会导致service不可用
            bag.Reset();
            result = proxy.GetName("aspark").GetAwaiter().GetResult();
            bag.Context.ShouldNotBeNull();
            bag.InvokeCount.ShouldBe(3);
            result.ShouldBe("not exists");
            bag.Context.Url.ShouldBe("http://ContractA/api/v1/name", StringCompareShould.IgnoreCase);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderUserAgent);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderTraceID);
            bag.Context.Extensions.ShouldContainKey(CobMvcDefaults.HeaderJump);
            bag.Context.Parameters.ShouldContainKey("id");
            bag.Context.Parameters["id"].ShouldBe("aspark");

            //3次重试后，已经无服务可用
            Should.Throw<Exception>(() => {
                proxy.SaveChanged(new Dto { Opt = 1 }).GetAwaiter().GetResult();//no service available
            });
        }

        [CobService(nameof(ContractA), Path = "/api/v1", ResolveServiceName = EnumResolveServiceName.KeepServiceName)]
        [CobRetryStrategy(Count = 2)]
        [RequestFilter(Name = "admin")]
        public interface ContractA
        {
            [CobService(Path = "/sys/score")]
            int GetScore(int id);

            [CobService(Path = "name")]
            [CobRetryStrategy(FallbackValue = "\"not exists\"")]
            Task<string> GetName(string id);

            [CobService(Timeout = 3)]
            [RequestFilter(Name = "aspark")]
            Task<bool> SaveChanged(Dto dto);
        }

        public class ServiceA : ContractA
        {
            public Task<string> GetName(string id)
            {
                //return Task.FromResult("@" + id);
                throw new NotImplementedException();//use defalut value

                //return Task.FromException<string>(new NotImplementedException());
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
        {
            public int Opt { get; set; }
        }

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
