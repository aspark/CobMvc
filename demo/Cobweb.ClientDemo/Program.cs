using Cobweb.Client;
using Cobweb.Core.Client;
using Cobweb.Demo.Contract;
using Microsoft.Extensions.DependencyInjection;
using System;
using Cobweb.Consul;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Cobweb.ClientDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("press Esc to exit!");

            StartMain().Wait();
        }

        static async Task StartMain()
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
            Console.WriteLine("5: Mark");

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
                        ret = await client.GetUserInfo("name" + DateTime.Now.Second);
                        break;
                    case '4':
                        await client.SaveUserInfo(new UserInfo { ID = DateTime.Now.Second, Name = "name", Addr = "" }).ContinueWith(t => {
                            Console.WriteLine("after SaveUserInfo...");
                        });
                        ret = null;
                        break;
                    case '5':
                        client.Mark(DateTime.Now.Second);
                        ret = null;
                        break;
                }

                Console.WriteLine("返回:{0}", JsonConvert.SerializeObject(ret));
            }
        }
    }
}
