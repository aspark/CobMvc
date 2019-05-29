using Cobweb.Client;
using Cobweb.Core.Client;
using Cobweb.Demo.Contract;
using Microsoft.Extensions.DependencyInjection;
using System;
using Cobweb.Consul;

namespace Cobweb.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            StartMain();

            Console.ReadKey();
        }

        static void StartMain()
        {
            var services = new ServiceCollection();

            services.AddCobweb(cob => {
                cob.UseConsul(opt => {
                    opt.Address = new Uri("http://localhost:8500");
                });
            });

            var provider = services.BuildServiceProvider();

            var strs = provider.GetService<ICobClientFactory>().GetProxy<IDemo>().GetNames();

            Console.WriteLine("返回:{0}", string.Join(", ", strs));
        }
    }
}
