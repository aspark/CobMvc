using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobMvc.Core;
using CobMvc.Core.Client;
using CobMvc.Demo.Contract;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CobMvc.Demo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase, IDemo
    {
        ICobClientFactory _clientFactory;

        ICobMvcContextAccessor _cobmvcContextAccessor;

        public TestController(ICobClientFactory clientFactory, ICobMvcContextAccessor cobmvcContextAccessor)
        {
            _cobmvcContextAccessor = cobmvcContextAccessor;
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public string Health()
        {
            return "on/off";
        }

        // GET api/values
        [HttpGet]
        [Route("/api/GetNames")]
        public string[] GetNames()
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke GetNames");

            return new string[] { time, time };
        }

        [HttpGet]
        public string[] GetOtherNames()
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke GetOtherNames");

            var names = _clientFactory.GetProxy<IDemo>().GetNames();

            return names;
        }

        // GET api/values/5
        [HttpGet]
        public Task<UserInfo> GetUserInfo(string name)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke GetUserInfo:{name}");
            return Task.FromResult(new UserInfo { Name = name, ID = 1, Addr = time });
        }

        [HttpPost]
        public Task SaveUserInfo(int operatorID, [FromBody]UserInfo user)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke SaveUserInfo:{operatorID}\t{JsonConvert.SerializeObject(user)}");

            user.Addr = time;

            return Task.FromResult(user);
        }

        [HttpPost]
        public Task SaveUserInfo2(int operatorID, UserInfo user1, [FromQuery]UserInfo user2)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke SaveUserInfo2:{operatorID}\t{JsonConvert.SerializeObject(user1)}\t{JsonConvert.SerializeObject(user2)}");

            user1.Addr = user2.Addr = time;

            return Task.FromResult(user1);
        }

        [HttpGet]
        public void Mark(int ms)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\t{_cobmvcContextAccessor.Current.TraceID}\tinvoke Mark");
        }

        [HttpGet]
        public string[] Fallback()
        {
            throw new NotImplementedException();
        }
    }
}
