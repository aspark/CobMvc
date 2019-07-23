using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobMvc.Core.Client;
using CobMvc.Demo.Shop.Contract;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CobMvc.Demo.Shop.ApiServer
{
    [Route("/")]
    public class ApiController : Controller
    {
        private ICobClientFactory _clientFactory = null;

        public ApiController(ICobClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        [Route("/gw/vapi/index")]
        public async Task<object> Index()
        {
            //var addr = _clientFactory.GetProxy<IUser>().GetAddress(userID);
            var user = _clientFactory.GetProxy<IUser>().GetUserInfo();
            var product = _clientFactory.GetProxy<IProduct>().GetProducts();

            await Task.WhenAll(user, product);

            return new {
                //addr = addr.Result, 
                user = user.Result,
                product = product.Result
            };
        }

        [HttpPost]
        [Route("/gw/api/order/CreateOrder")]
        public async Task<object> CreateOrder([FromBody]CreateOrderDto order)
        {
            var result = await _clientFactory.GetProxy<IOrder>().CreateOrder(order);

            return result;
        }

    }
}
