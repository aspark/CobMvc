using CobMvc.Core;
using System;
using System.Threading.Tasks;

namespace CobMvc.Demo.Shop.Contract
{
    [CobService("CobMvc.Demo.Shop.Product")]
    public interface IProduct
    {
        Task<ApiResult<ProductDto[]>> GetProducts();

        [CobRetryStrategy(Count = 3, FallbackHandler = typeof(CheckStockFallbackHandler))]//FallbackValue = "Task.FromResult(ApiResult.Create<bool>(false))"
        Task<ApiResult<bool>> CheckStock(Guid productID);
    }

    internal class CheckStockFallbackHandler : ICobFallbackHandler
    {
        public object GetValue(CobFallbackHandlerContext context)
        {
            return Task.FromResult(ApiResult.Create<bool>(false));
        }
    }

    public class ProductDto
    {
        public Guid ID { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public string Desc { get; set; }
    }
}
