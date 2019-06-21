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

namespace CobMvc.ClientDemo
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
                });
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
