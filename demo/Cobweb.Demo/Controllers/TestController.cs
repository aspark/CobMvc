using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cobweb.Core;
using Cobweb.Core.Client;
using Cobweb.Demo.Contract;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Cobweb.Demo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase, IDemo
    {
        ICobClientFactory _clientFactory;

        ICobwebContextFactory _cobwebContextFactory;

        public TestController(ICobClientFactory clientFactory, ICobwebContextFactory cobwebContextFactory)
        {
            _cobwebContextFactory = cobwebContextFactory;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public string Health()
        {
            return "on/off";
        }

        // GET api/values
        [HttpGet]
        public string[] GetNames()
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobwebContextFactory.Current.TraceID}\tinvoke GetNames");

            return new string[] { time, time };
        }

        [HttpGet]
        public string[] GetOtherNames()
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobwebContextFactory.Current.TraceID}\tinvoke GetOtherNames");

            var names = _clientFactory.GetProxy<IDemo>().GetNames();

            return names;
        }

        // GET api/values/5
        [HttpGet]
        public Task<UserInfo> GetUserInfo(string name)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobwebContextFactory.Current.TraceID}\tinvoke GetUserInfo:{name}");
            return Task.FromResult(new UserInfo { Name = name, ID = 1, Addr = time });
        }

        [HttpPost]
        public Task SaveUserInfo(UserInfo user)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobwebContextFactory.Current.TraceID}\tinvoke SaveUserInfo:{JsonConvert.SerializeObject(user)}");

            user.Addr = time;

            return Task.FromResult(user);
        }

        [HttpGet]
        public void Mark(int ms)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobwebContextFactory.Current.TraceID}\tinvoke Mark");
        }
    }
}
