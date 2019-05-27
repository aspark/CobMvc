using Cobweb.Core.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Cobweb
{
    public static class WebExtensions
    {
        /// <summary>
        /// 启用服务发现
        /// </summary>
        public static void ConfigureCobweb(this IWebHostBuilder webHostBuilder)
        {
            //todo:注册服务

            //todo:添加服务获取类
        }

        public static void AddCobweb(this IServiceCollection services)
        {
            //添加默认的配置
            //services
        }

        public static IApplicationBuilder UseCobweb(this IApplicationBuilder mvcBuilder, string serviceName)
        {
            var hostBuilder = mvcBuilder.ApplicationServices.GetRequiredService<IWebHostBuilder>();
            var host = hostBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
            var uri = new Uri(host);

            var reg = mvcBuilder.ApplicationServices.GetRequiredService<IServiceRegistration>();

            //IService
            //var services = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(t => typeof(IService).IsAssignableFrom(t)));

            reg.Register(new ServiceInfo
            {
                Host = uri.ToString(),
                Name = serviceName,
                ID = host,
                Port = uri.Port,
                CheckInfoes = new[] {
                    new ServiceCheckInfo{
                        Type = ServiceCheckInfoType.Http,
                        Target = uri,
                        Interval = TimeSpan.FromSeconds(3),
                        Timeout = TimeSpan.FromSeconds(60)
                    }
                }
            });

            return mvcBuilder;
        }
    }
}
