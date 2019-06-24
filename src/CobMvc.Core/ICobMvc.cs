using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CobMvc.Core
{
    public interface ICobMvc
    {
        void ConfigureServices(Action<IServiceCollection> service);

        //config options??

        void Configure<T>(Action<T> options) where T : class;

        void ConfigureOptions(Action<CobMvcOptions> options);


    }
}
