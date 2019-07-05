using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CobMvc.Demo.Shop.Order
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
                    //config.AddJsonFile("");
                }).ConfigureLogging(config => {
                    config.AddConsole();
                    config.SetMinimumLevel(LogLevel.Information);
                })
                .UseStartup<Startup>();
    }
}
