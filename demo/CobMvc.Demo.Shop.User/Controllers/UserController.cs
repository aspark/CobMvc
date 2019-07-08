using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobMvc.Demo.Shop.Contract;
using Microsoft.AspNetCore.Mvc;

namespace CobMvc.Demo.Shop.User.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class UserController : ControllerBase, IUser
    {
        [HttpGet]
        public string Check()
        {
            return "on";
        }

        // GET api/values
        [HttpGet]
        public Task<ApiResult<Address[]>> GetAddress(int userID)
        {
            return Task.FromResult(ApiResult.Create(new[]{
                new Address()
                {
                    Province = "广东",
                    City = "深圳"
                },new Address()
                {
                    Province = "我的老家",
                    City = "这个屯"
                },
            }));
        }
    }
}
