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


        [HttpGet]
        public async Task<ApiResult<UserInfo>> GetUserInfo()
        {
            var user = new UserInfo()
            {
                Name = "张三"
            };

            user.Address = (await GetAddress(user.ID)).Data;

            return ApiResult.Create(user);
        }

        [HttpGet]
        public Task<ApiResult<Address[]>> GetAddress(Guid userID)
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
