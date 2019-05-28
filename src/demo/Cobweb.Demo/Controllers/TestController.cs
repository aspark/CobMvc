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
        // GET api/values
        [HttpGet]
        public string[] GetNames()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet]
        public Task<UserInfo> GetUserInfo(string name)
        {
            return Task.FromResult(new UserInfo { Name = name, ID = 1, Addr = "addr" });
        }

        // POST api/values
        [HttpPost]
        public void Save([FromBody] string value)
        {
        }

    }
}
