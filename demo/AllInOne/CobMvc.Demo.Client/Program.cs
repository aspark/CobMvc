using CobMvc.Client;
using CobMvc.Core.Client;
using CobMvc.Demo.Contract;
using Microsoft.Extensions.DependencyInjection;
using System;
using CobMvc.Consul;
using CobMvc.Consul.Configuration;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CobMvc.WebSockets;
using CobMvc.Core;

namespace CobMvc.Demo.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("press Esc to exit!");
            var services = new ServiceCollection();

            services.AddCobMvc(cob => {
                cob.AddConsul(opt => {
                    opt.Address = new Uri("http://localhost:8500");
                }).AddCobWebSockets()
                .ConfigureOptions(opt => opt.EnableCobMvcParametersBinder = true);
                cob.Configure<CobMvcRequestOptions>(opt => opt.MaxConnetions = 300);
            });

            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json");//add json file for default value
            configBuilder.AddConsul(c => c.Address = new Uri("http://localhost:8500"));
            var config = configBuilder.Build();

            //var setting = new Settings();
            //config.GetSection("current");
            //config.Bind("current", setting);

            services.AddSingleton<IConfiguration>(config);

            services.AddTransient<Business>();

            services.Configure<Settings>(config.GetSection("current"));

            var provider = services.BuildServiceProvider();


            provider.GetService<Business>().StartMain().Wait();
        }
    }

    public class Business
    {
        IServiceProvider _serviceProvider = null;
        IOptionsMonitor<Settings> _settings = null;

        public Business(IServiceProvider serviceProvider, IOptionsMonitor<Settings> settings)
        {
            _settings = settings;
            Console.WriteLine("current settings:" + JsonConvert.SerializeObject(settings.CurrentValue));
            _settings.OnChange((s, n) => Console.WriteLine("new settings:" + JsonConvert.SerializeObject(s)));
            _serviceProvider = serviceProvider;
        }

        public async Task StartMain()
        {
            var services = new ServiceCollection();

            services.AddCobMvc(cob => {
                cob.AddConsul(opt => {
                    opt.Address = new Uri("http://localhost:8500");
                });
            });

            var client = _serviceProvider.GetService<ICobClientFactory>().GetProxy<IDemo>();

            Console.WriteLine("pls select(Esc to exit):");
            Console.WriteLine("1: GetNames");
            Console.WriteLine("2: GetOtherNames");
            Console.WriteLine("3: GetUserInfo");
            Console.WriteLine("4: SaveUserInfo");
            Console.WriteLine("5: Mark");
            Console.WriteLine("6: Fallback");
            Console.WriteLine("7: SaveUserInfo2");

            var rnd = new Random(Guid.NewGuid().GetHashCode());
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey()).Key != ConsoleKey.Escape)
            {
                object ret = null;

                switch (key.KeyChar)
                {
                    case '1':
                    default:
                        ret = client.GetNames();
                        //ret = provider.GetService<ICobClientFactory>().GetProxy(new CobServiceDescriptor { ServiceName = "CobMvcDemo" }).Invoke<string[]>("GetNames", null, null);\
                        break;
                    case '2':
                        ret = client.GetOtherNames();
                        break;
                    case '3':
                        ret = await client.GetUserInfo("name" + rnd.Next());
                        break;
                    case '4':
                        await client.SaveUserInfo(rnd.Next(), new UserInfo { ID = rnd.Next(), Name = "name", Addr = "" }).ContinueWith(t => {
                            Console.WriteLine("after SaveUserInfo...");
                        });
                        ret = null;
                        break;
                    case '5':
                        client.Mark(rnd.Next());
                        ret = null;
                        break;
                    case '6':
                        ret = client.Fallback();
                        break;
                    case '7':
                        await client.SaveUserInfo2(rnd.Next(), new UserInfo { ID = rnd.Next(), Name = "name", Addr = "" }, new UserInfo { ID = rnd.Next(), Name = "name", Addr = "" }).ContinueWith(t => {
                            Console.WriteLine("after SaveUserInfo...");
                        });
                        break;
                }

                Console.WriteLine("返回:{0}", JsonConvert.SerializeObject(ret));
            }
        }
    }

    //consul kv values(string or json):
    //CobMvc/Configuration/current/db="value"
    //CobMvc/Configuration/current/auth={token:3, expired:"01:00:00"}
    public class Settings
    {
        public string DB { get; set; }

        public Auth auth { get; set; }
    }

    public class Auth
    {
        public string Token { get; set; }


        public TimeSpan Expired { get; set; }
    }
}
