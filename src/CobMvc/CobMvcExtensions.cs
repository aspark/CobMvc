using CobMvc.Client;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Core.Common;
using CobMvc.Core.Service;
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

namespace CobMvc
{
    public static class CobMvcExtensions
    {
        /// <summary>
        /// 添加cobmvc服务发现、调用等相关服务
        /// </summary>
        public static IMvcBuilder AddCobMvc(this IMvcBuilder mvcBuilder, Action<ICobMvc> setup)
        {
            mvcBuilder.Services.AddSingleton(mvcBuilder);

            var container = new DefaultCobMvc(mvcBuilder.Services);

            setup?.Invoke(container);

            container.ApplyConfigure();

            ServicesExtensions.EnsureServerServices(mvcBuilder.Services);

            return mvcBuilder;
        }


        public static IMvcBuilder AddCobMvc(this IMvcBuilder mvcBuilder)
        {
            return mvcBuilder.AddCobMvc(cob=> { });
        }

        //public static IApplicationBuilder UseCobMvc<T>(this IApplicationBuilder mvcBuilder, Action<CobMvcStartupOptions> optionSetup = null)//, Action<CobMvcOptions> option
        //{
        //    return mvcBuilder.UseCobMvc(opt=>
        //    {
        //        opt.ServiceName = typeof(T).Assembly.GetName().Name;
        //        optionSetup?.Invoke(opt);
        //    });
        //}

        /// <summary>
        /// 启用cobmvc服务注册，放置在UseMvc()之前
        /// </summary>
        /// <param name="mvcBuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCobMvc(this IApplicationBuilder mvcBuilder, Action<CobMvcOptions> optionOverride = null)//
        {
            var options = mvcBuilder.ApplicationServices.GetService<IOptions<CobMvcOptions>>().Value;

            optionOverride?.Invoke(options);

            if (string.IsNullOrWhiteSpace(options.ServiceAddress))
            {
                var addr = mvcBuilder.ServerFeatures.Get<IServerAddressesFeature>();
                options.ServiceAddress = addr.Addresses.First();
#if !DEBUG
                options.ServiceAddress = NetHelper.ChangeToExternal(options.ServiceAddress);
#endif
            }

            //如果没有设置服务名称，则使用程序集作为服务名
            if(string.IsNullOrWhiteSpace(options.ServiceName))
            {
                options.ServiceName = Assembly.GetEntryAssembly().GetName().Name;
            }

            var uri = new Uri(options.ServiceAddress);

            var svcInfo = new ServiceInfo
            {
                Address = uri.ToString(),
                Name = options.ServiceName,
                ID = StringHelper.ToMD5(options.ServiceAddress + options.ServiceName),//
                Port = uri.Port
            };

            if(!string.IsNullOrWhiteSpace(options.HealthCheck))
            {
                svcInfo.CheckInfoes = new[] {
                    new ServiceCheckInfo{
                        Type = ServiceCheckInfoType.Http,
                        Target = new Uri(UriHelper.Combine(options.ServiceAddress, options.HealthCheck)),
                        Interval = TimeSpan.FromSeconds(3),
                        Timeout = TimeSpan.FromSeconds(60)
                    }
                };
            }

            var logger = mvcBuilder.ApplicationServices.GetRequiredService<ILoggerFactory>().CreateLogger<CobMvcMiddleware>();
            logger.LogDebug("register service:{0}\t{1}", svcInfo.Name, svcInfo.Address);

            var reg = mvcBuilder.ApplicationServices.GetRequiredService<IServiceRegistration>();
            reg.Register(svcInfo);

            mvcBuilder.UseMiddleware<CobMvcMiddleware>();

            //todo:deregister

            return mvcBuilder;
        }
    }
}
