using CobMvc.Core;
using System;
using System.Threading.Tasks;

namespace CobMvc.Demo.Shop.Contract
{
    [CobService("CobMvc.Demo.Shop.User", Path = "/api/user")]
    public interface IUser
    {
        Task<ApiResult<UserInfo>> GetUserInfo();

        Task<ApiResult<Address[]>> GetAddress(Guid userID);
    }

    public class UserInfo
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public Address[] Address { get; set; }
    }

    public class Address
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public string Province { get; set; }

        public string City { get; set; }

        //..
    }
}
