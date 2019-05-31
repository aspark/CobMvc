using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cobweb.Demo.Contract;
using Microsoft.AspNetCore.Mvc;

namespace Cobweb.Demo.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase, IDemo
    {
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

            Console.WriteLine($"{time}\tinvoke GetNames");

            return new string[] { time, time };
        }

        // GET api/values/5
        [HttpGet]
        public Task<UserInfo> GetUserInfo(string name)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{time}\tinvoke GetUserInfo");
            return Task.FromResult(new UserInfo { Name = name, ID = 1, Addr = time });
        }

        [HttpPost]
        public Task<UserInfo> SaveUserInfo(UserInfo user)
        {
            var time = DateTime.Now.ToString("HH:mm:ss.ffff");

            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.ffff")}\tinvoke SaveUserInfo");

            user.Addr = time;

            return Task.FromResult(user);
        }
    }
}
