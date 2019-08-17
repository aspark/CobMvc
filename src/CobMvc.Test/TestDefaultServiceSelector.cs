using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Shouldly;
using CobMvc.Client;
using Microsoft.Extensions.Logging;
using CobMvc.Core.InMemory;
using System.Linq;

namespace CobMvc.Test
{
    public class TestDefaultServiceSelector
    {
        [Fact]
        public void TestSelector()
        {
            var registration = new InMemoryServiceRegistration();

            var a = new Core.Service.ServiceInfo { Name = "name", Address = "http:/a.com", ID = "a", Status = Core.Service.ServiceInfoStatus.Healthy };
            registration.Register(a);

            var b = new Core.Service.ServiceInfo { Name = "name", Address = "http:/b.com", ID = "b", Status = Core.Service.ServiceInfoStatus.Healthy };
            registration.Register(b);
            var selector = new DefaultServiceSelector(registration, "name", new LoggerFactory().CreateLogger<DefaultServiceSelector>());

            var last = selector.GetOne();
            last.ShouldNotBeNull();

            var service = selector.GetOne();
            service.ID.ShouldNotBe(last.ID);
            last = service;

            service = selector.GetOne();
            service.ID.ShouldNotBe(last.ID);
            last = service;

            var failed = service.ID == b.ID ? a : b;
            while(failed.Status == Core.Service.ServiceInfoStatus.Healthy)
                selector.MarkServiceFailed(failed, true);
            selector.GetOne().ID.ShouldBe(last.ID);
            selector.GetOne().ID.ShouldBe(last.ID);

            selector.MarkServiceHealthy(failed, TimeSpan.FromSeconds(0));
            failed.Status = Core.Service.ServiceInfoStatus.Healthy;

            service = selector.GetOne();
            if(service.ID == last.ID)
                service = selector.GetOne();
            service.ID.ShouldNotBe(last.ID);
            last = service;

            service = selector.GetOne();
            service.ID.ShouldNotBe(last.ID);
            last = service;

            while (a.Status == Core.Service.ServiceInfoStatus.Healthy)
                selector.MarkServiceFailed(a, true);
            while (b.Status == Core.Service.ServiceInfoStatus.Healthy)
                selector.MarkServiceFailed(b, true);
            selector.GetOne().ShouldBeNull();

            selector.MarkServiceHealthy(failed, TimeSpan.FromSeconds(0));
            failed.Status = Core.Service.ServiceInfoStatus.Healthy;
            selector.GetOne().ShouldNotBeNull();
        }
    }
}
