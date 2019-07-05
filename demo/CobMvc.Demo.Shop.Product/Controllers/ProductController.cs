using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CobMvc.Demo.Shop.Contract;
using Microsoft.AspNetCore.Mvc;

namespace CobMvc.Demo.Shop.Product.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase, IProduct
    {
        [HttpGet]
        public string Check()
        {
            return "on";
        }

        [HttpGet]
        public Task<ApiResult<ProductDto[]>> GetProducts()
        {
            return Task.FromResult(ApiResult.Create(new[] {
                new ProductDto{ Name="商品1", Desc="不可描述" },
                new ProductDto{ Name="商品2", Desc="不可描述" },
                new ProductDto{ Name="商品3", Desc="不可描述" },
                new ProductDto{ Name="商品4", Desc="不可描述" },
                new ProductDto{ Name="商品5", Desc="不可描述" },
                new ProductDto{ Name="商品6", Desc="不可描述" },
                new ProductDto{ Name="商品7", Desc="不可描述" },
            }));
        }

        [HttpGet]
        public Task<ApiResult<bool>> CheckStock(Guid productID)
        {
            return Task.FromResult(ApiResult.Create(true));
        }
    }
}
