using Cobweb.Client;
using Cobweb.Core;
using Cobweb.Core.Client;
using Cobweb.Core.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;

namespace Cobweb
{
    public static class CobwebExtensions
    {
        /// <summary>
        /// 添加cobweb相关服务
        /// </summary>
        public static IMvcBuilder AddCobweb(this IMvcBuilder mvcBuilder, Action<ICobweb> setup)
        {
            var container = new DefaultCobweb(mvcBuilder.Services);

            setup?.Invoke(container);

            container.ApplyConfigure();

            ServicesExtensions.RegisterDefaultServices(mvcBuilder.Services);

            return mvcBuilder;
        }


        public static IMvcBuilder AddCobweb(this IMvcBuilder mvcBuilder)
        {
            return mvcBuilder.AddCobweb(cob=> { });
        }

        //public static IApplicationBuilder UseCobweb<T>(this IApplicationBuilder mvcBuilder, Action<CobwebStartupOptions> optionSetup = null)//, Action<CobwebOptions> option
        //{
        //    return mvcBuilder.UseCobweb(opt=>
        //    {
        //        opt.ServiceName = typeof(T).Assembly.GetName().Name;
        //        optionSetup?.Invoke(opt);
        //    });
        //}

        /// <summary>
        /// 启用cobweb，放置在UseMvc()之前
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCobweb(this IApplicationBuilder mvcBuilder, Action<CobwebOptions> optionOverride = null)//
        {
            var options = mvcBuilder.ApplicationServices.GetService<IOptions<CobwebOptions>>().Value;

            optionOverride?.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.ServiceAddress))
            {
                var addr = mvcBuilder.ServerFeatures.Get<IServerAddressesFeature>();
                options.ServiceAddress = addr.Addresses.First();
            }

            var uri = new Uri(options.ServiceAddress);

            var svcInfo = new ServiceInfo
            {
                Address = uri.ToString(),
                Name = options.ServiceName,
                ID = options.ServiceAddress,//todo:transform to alpha
                Port = uri.Port
            };

            if(!string.IsNullOrWhiteSpace(options.HealthCheck))
            {
                svcInfo.CheckInfoes = new[] {
                    new ServiceCheckInfo{
                        Type = ServiceCheckInfoType.Http,
                        Target = new Uri(options.ServiceAddress.TrimEnd('/') + "/" + options.HealthCheck.TrimStart('/')),
                        Interval = TimeSpan.FromSeconds(3),
                        Timeout = TimeSpan.FromSeconds(60)
                    }
                };
            }

            var logger = mvcBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger<CobwebMiddleware>();
            logger.LogInformation("register service:{0}\t{1}", svcInfo.Name, svcInfo.Address);

            var reg = mvcBuilder.ApplicationServices.GetRequiredService<IServiceRegistration>();
            reg.Register(svcInfo);

            mvcBuilder.UseMiddleware<CobwebMiddleware>();

            //todo:deregister

            return mvcBuilder;
        }
    }
}
