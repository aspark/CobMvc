using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Demo.Shop.Contract
{
    public class ApiResult
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public static ApiResult<T> Create<T>(T obj)
        {
            return new ApiResult<T>() {
                IsSuccess = true,
                Data = obj
            };
        }

        public static ApiResult<T> Error<T>(string msg)
        {
            return new ApiResult<T>()
            {
                IsSuccess = false,
                Message = msg
            };
        }
    }

    public class ApiResult<T>: ApiResult
    {
        public T Data { get; set; }
    }
}
