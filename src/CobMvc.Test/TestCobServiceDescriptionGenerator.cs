using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Shouldly;
using CobMvc.Core;
using CobMvc.Core.Client;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CobMvc.Core.Service;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CobMvc.Test
{
    public class TestCobServiceDescriptionGenerator
    {
        private ServiceInfo _service = new Core.Service.ServiceInfo() { Name = "name", Address = "http://cobmvc.test/" };

        private IConfiguration GetConfiguration()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"));

            return builder.Build();
        }

        [Fact]
        public void TestGeneratorSimple()
        {
            var options = Options.Create(new CobMvcRequestOptions());

            var desc = new CobServiceDescriptionGenerator(options, GetConfiguration()).Create<ContractA>() as CobServiceClassDescription;

            desc.ShouldNotBeNull();
            desc.ServiceName.ShouldBe(nameof(ContractA));
            desc.Path.ShouldBe("/api/v1");
            desc.Timeout.ShouldBe(TimeSpan.FromSeconds(options.Value.DefaultTimeout));
            desc.ResolveServiceName.ShouldBe(EnumResolveServiceName.ResolveServiceName);
            desc.RetryTimes.ShouldBe(2);
            desc.Filters.Length.ShouldBe(1);

            var action = typeof(ContractA).GetMethod(nameof(ContractA.GetScore));
            var actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.Path.ShouldBe("/sys/score");
            actionDesc.Timeout.ShouldBe(TimeSpan.FromSeconds(options.Value.DefaultTimeout));
            actionDesc.Transport.ShouldBe(CobRequestTransports.Http);
            actionDesc.FallbackValue.ShouldBeNullOrWhiteSpace();
            actionDesc.Filters.Length.ShouldBe(3);

            action = typeof(ContractA).GetMethod(nameof(ContractA.GetName));
            actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.Path.ShouldBe("/api/v1/name");
            actionDesc.FallbackValue.ShouldBe("not exists");
            actionDesc.Filters.Length.ShouldBe(1);

            action = typeof(ContractA).GetMethod(nameof(ContractA.SaveChanged));
            actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.ShouldBeAssignableTo(typeof(CobServiceActionDescription));
            actionDesc.Path.ShouldBe(null);
            (actionDesc as CobServiceActionDescription).GetUrl(_service).ShouldBe("http://cobmvc.test/api/v1/SaveChanged");
            actionDesc.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
            actionDesc.ResolveServiceName.ShouldBe(EnumResolveServiceName.KeepServiceName);
            actionDesc.Transport.ShouldBe(CobRequestTransports.WebSocket);
            actionDesc.Filters.Length.ShouldBe(2);
        }

        [CobService(nameof(ContractA), Path = "/api/v1")]
        [CobRetryStrategy(Count = 2)]
        [RequestFilter(Name = "1")]
        public interface ContractA
        {
            [CobService(Path = "/sys/score")]
            [RequestFilter(Name = "2")]
            [RequestFilter(Name = "3")]
            int GetScore(int id);

            [CobService(Path = "name")]
            [CobRetryStrategy(FallbackValue = "not exists")]
            Task<string> GetName(string id);

            [CobService(Timeout = 3, ResolveServiceName = EnumResolveServiceName.KeepServiceName, Transport = CobRequestTransports.WebSocket)]
            [RequestFilter(Name = "4")]
            Task SaveChanged(Dto dto);
        }

        [Fact]
        public void TestGeneratorOverrideConfig()
        {
            var options = Options.Create(new CobMvcRequestOptions());

            var desc = new CobServiceDescriptionGenerator(options, GetConfiguration()).Create<ContractB>() as CobServiceClassDescription;

            desc.ShouldNotBeNull();
            desc.ServiceName.ShouldBe(nameof(ContractB));
            desc.Path.ShouldBe("/api");
            desc.Transport.ShouldBe(CobRequestTransports.Http);
            desc.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
            desc.ResolveServiceName.ShouldBe(EnumResolveServiceName.KeepServiceName);
            desc.RetryTimes.ShouldBe(2);
            desc.Filters.Length.ShouldBe(0);

            var action = typeof(ContractB).GetMethod(nameof(ContractB.GetScore));
            var actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.ShouldBeAssignableTo(typeof(CobServiceActionDescription));
            actionDesc.Path.ShouldBe("/sys/score");
            (actionDesc as CobServiceActionDescription).GetUrl(_service).ShouldBe("http://cobmvc.test/sys/score");
            actionDesc.Timeout.ShouldBe(TimeSpan.FromSeconds(3));
            actionDesc.ResolveServiceName.ShouldBe(EnumResolveServiceName.ResolveServiceName);
            actionDesc.Transport.ShouldBe(CobRequestTransports.WebSocket);
            actionDesc.FallbackValue.ShouldBeNullOrWhiteSpace();
            actionDesc.RetryTimes.ShouldBe(3);
            actionDesc.Filters.Length.ShouldBe(2);

            action = typeof(ContractB).GetMethod(nameof(ContractB.GetName));
            actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.ShouldBeAssignableTo(typeof(CobServiceClassDescription));
            actionDesc.Path.ShouldBe("/api");
            (actionDesc as CobServiceClassDescription).GetUrl(_service, action).ShouldBe("http://name/api/GetName");
            actionDesc.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
            actionDesc.ResolveServiceName.ShouldBe(EnumResolveServiceName.KeepServiceName);
            actionDesc.Transport.ShouldBe(CobRequestTransports.Http);
            actionDesc.Filters.Length.ShouldBe(0);

            action = typeof(ContractB).GetMethod(nameof(ContractB.SaveChanged));
            actionDesc = desc.GetActionOrTypeDesc(action);
            actionDesc.ShouldBeAssignableTo(typeof(CobServiceActionDescription));
            actionDesc.Path.ShouldBe(null);
            (actionDesc as CobServiceActionDescription).GetUrl(_service).ShouldBe("http://name/api/SaveChanged");
            actionDesc.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
            actionDesc.ResolveServiceName.ShouldBe(EnumResolveServiceName.KeepServiceName);
            actionDesc.Transport.ShouldBe(CobRequestTransports.Http);
            actionDesc.Filters.Length.ShouldBe(1);
        }

        [CobService(nameof(ContractB), Path = "/api", ResolveServiceName = EnumResolveServiceName.KeepServiceName, Timeout = 10, Transport = CobRequestTransports.Http)]
        [CobRetryStrategy(Count = 2)]
        public interface ContractB
        {
            [CobService(Path = "/sys/score", Timeout = 3, ResolveServiceName = EnumResolveServiceName.ResolveServiceName, Transport = CobRequestTransports.WebSocket)]
            [CobRetryStrategy(Count = 3)]
            [RequestFilter(Name = "2")]
            [RequestFilter(Name = "3")]
            int GetScore(int id);

            Task<string> GetName(string id);

            [RequestFilter(Name = "4")]
            Task SaveChanged(Dto dto);
        }

        public class Dto
        { }

        public class RequestFilter : CobRequestFilterAttribute
        {
            public string Name { get; set; }
        }

        [Fact]
        public void TestGeneratorFromConfig()
        {
            var options = Options.Create(new CobMvcRequestOptions());

            var desc = new CobServiceDescriptionGenerator(options, GetConfiguration()).Create<ContractC>() as CobServiceClassDescription;

            desc.ShouldNotBeNull();
            desc.ServiceName.ShouldBe("ConfigContractC");
            desc.Path.ShouldBe("api/config");
            desc.Transport.ShouldBe(CobRequestTransports.Http);
            desc.Timeout.ShouldBe(TimeSpan.FromSeconds(5.2));
            desc.ResolveServiceName.ShouldBe(EnumResolveServiceName.KeepServiceName);
            desc.RetryTimes.ShouldBe(2);
            desc.Filters.Length.ShouldBe(0);
        }

        //[CobService(nameof(ContractB), Path = "/api", ResolveServiceName = EnumResolveServiceName.KeepServiceName, Timeout = 10, Transport = CobRequestTransports.Http)]
        [CobServiceFromConfig("services:ContractC")]
        [CobRetryStrategy(Count = 2)]
        public interface ContractC
        {
            [CobService(Path = "/sys/score", Timeout = 3, ResolveServiceName = EnumResolveServiceName.ResolveServiceName, Transport = CobRequestTransports.WebSocket)]
            [CobRetryStrategy(Count = 3)]
            [RequestFilter(Name = "2")]
            [RequestFilter(Name = "3")]
            int GetScore(int id);

            Task<string> GetName(string id);

            [RequestFilter(Name = "4")]
            Task SaveChanged(Dto dto);
        }
    }
}
