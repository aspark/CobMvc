using CobMvc.Core;
using System;
using System.Threading.Tasks;

namespace CobMvc.Demo.Shop.Contract
{
    [CobService("CobMvc.Demo.Shop.Order")]
    public interface IOrder
    {
        Task<ApiResult<OrderDto>> CreateOrder(CreateOrderDto order);
    }

    public class CreateOrderDto
    {
        public Guid UserID { get; set; }

        public OrderDetailDto[] Details { get; set; }

        public Guid Address { get; set; }
    }

    public class OrderDetailDto
    {
        public Guid ProductID { get; set; }

        public string ProductName { get; set; }

        public int Quality { get; set; }
    }

    public class OrderDto : CreateOrderDto
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public DateTime CreateTime { get; set; } = DateTime.Now;
    }
}
