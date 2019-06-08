using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CobMvc;
using CobMvc.Consul;

namespace CobMvc.Demo
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddCobMvc(cob=> {
                    cob.ConfigureOptions(opt=> {
                        opt.ServiceName = "CobMvcDemo";
                        //opt.ServiceAddress = "http://localhost:54469";//改为console随机端口
                        opt.HealthCheck = "/api/test/Health";
                    });
                    cob.UseConsul(opt=> {
                        opt.Address = new Uri("http://localhost:8500");
                    });
                });

            //services.AddCobMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCobMvc();
            app.UseMvc();
        }
    }
}
