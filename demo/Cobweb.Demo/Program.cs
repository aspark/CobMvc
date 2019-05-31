using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cobweb.Core.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cobweb.Demo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            //WebHost.CreateDefaultBuilder(args)
            //    .UseStartup<Startup>();

            var builder = new WebHostBuilder();
            builder
            .ConfigureServices(services => {

            })
            .ConfigureLogging(log =>
            {
                log.ClearProviders();
                log.AddConsole();
                log.SetMinimumLevel(LogLevel.Error);
            })
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseUrls($"http://localhost:{NetHelper.GetAvailablePort()}")
            .UseStartup<Startup>();
            
            return builder;
        }
    }
}
