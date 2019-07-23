using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobMvc.Core.Client;
using CobMvc.Demo.Shop.Contract;
using Microsoft.AspNetCore.Mvc;

namespace CobMvc.Demo.Shop.Order.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderController : ControllerBase, IOrder
    {
        ICobClientFactory _clientFactory = null;

        public OrderController(ICobClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet]
        public string Check()
        {
            return "on";
        }

        [HttpPost]
        public async Task<ApiResult<OrderDto>> CreateOrder([FromBody]CreateOrderDto order)
        {
            foreach(var item in order.Details)
            {
                var stock = await _clientFactory.GetProxy<IProduct>().CheckStock(item.ProductID);
                if (stock == null || !stock.IsSuccess || !stock.Data)
                {
                    return ApiResult.Error<OrderDto>("库存不足");
                }
            }

            return ApiResult.Create(new OrderDto()
            {
                Address = order.Address,
                Details = order.Details
            });
        }
    }
}
