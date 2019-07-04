using CobMvc.Core;
using System;
using System.Threading.Tasks;

namespace CobMvc.Demo.Shop.Contract
{
    [CobService("CobMvc.Demo.Shop.User")]
    public interface IUser
    {
        Task<ApiResult<Address[]>> GetAddress(int userID);
    }

    public class Address
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public string Province { get; set; }

        public string City { get; set; }

        //..
    }
}
