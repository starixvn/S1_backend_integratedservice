using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartyBetHistory.Service;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.DAL;

namespace ThirdPartyBetHistory
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            /*Library*/
            //services.AddSingleton<Tools>();
            //services.AddSingleton<Common>();
            //services.AddSingleton<IBCDBLibrary>();

            /*DAL*/
            services.AddSingleton<ThirdPartyService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();

            ThirdPartyBetHelperBase.proxyHost = Configuration.GetValue<string>("ProxyHost");
            ThirdPartyBetHelperBase.proxyPort = Configuration.GetValue<int>("ProxyPort");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });
            });
        }
    }
}
