using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

//使有mvc手动实现在的网关
namespace CobMvc.Demo.Shop.ApiServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => {
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                    //config.AddJsonFile("");
                }).ConfigureLogging(config => {
                    config.AddConsole();
                    config.SetMinimumLevel(LogLevel.Information);
                })
                .UseStartup<Startup>();
    }
}
