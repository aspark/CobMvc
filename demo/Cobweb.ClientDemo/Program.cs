using Cobweb.Client;
using Cobweb.Core.Client;
using Cobweb.Demo.Contract;
using Microsoft.Extensions.DependencyInjection;
using System;
using Cobweb.Consul;
using Newtonsoft.Json;

namespace Cobweb.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("press Esc to exit!");

            StartMain();
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
            var client = provider.GetService<ICobClientFactory>().GetProxy<IDemo>();

            Console.WriteLine("pls select(Esc to exit):");
            Console.WriteLine("1: GetNames");
            Console.WriteLine("2: GetOtherNames");
            Console.WriteLine("3: GetUserInfo");
            Console.WriteLine("4: SaveUserInfo");

            ConsoleKeyInfo key;
            while ((key = Console.ReadKey()).Key != ConsoleKey.Escape)
            {
                object ret = null;

                switch (key.KeyChar)
                {
                    case '1':
                    default:
                        ret = client.GetNames();
                        //ret = provider.GetService<ICobClientFactory>().GetProxy(new CobServiceDescriptor { ServiceName = "CobwebDemo" }).Invoke<string[]>("GetNames", null, null);\
                        break;
                    case '2':
                        ret = client.GetOtherNames();
                        break;
                    case '3':
                        ret = client.GetUserInfo("name" + DateTime.Now.Second);
                        break;
                    case '4':
                        ret = client.SaveUserInfo(new UserInfo { ID = DateTime.Now.Second, Name="name", Addr = "" });
                        break;
                }

                Console.WriteLine("返回:{0}", JsonConvert.SerializeObject(ret));
            }
        }
    }
}
