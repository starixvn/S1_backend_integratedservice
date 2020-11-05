using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(configuration.GetValue<string>("ProxyHost"), configuration.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }
            /*Library*/
            services.AddSingleton<HttpHelper>();
            services.AddSingleton<Common>();
            services.AddSingleton<DateTimeHelper>();
            services.AddSingleton<Log2File>();

            /*DAL*/
            services.AddSingleton<ThirdPartyHelper>();
            services.AddSingleton<WagerDGService>();
            services.AddSingleton<WagerAllBetService>();
            services.AddSingleton<WagerSAService>();
            services.AddSingleton<WagerAGService>();
            services.AddSingleton<WagerCFService>();
            services.AddSingleton<WagerSGService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            // DG
            services.AddSingleton<WagerDGBLL>();
            services.AddSingleton<ApiDGBLL>();
            services.AddSingleton<WagerRedoDGBLL>();
            // AllBet
            services.AddSingleton<WagerAllBetBLL>();
            services.AddSingleton<ApiAllBetBLL>();
            services.AddSingleton<WagerRedoAllBetBLL>();
            // SA
            services.AddSingleton<WagerSABLL>();
            services.AddSingleton<ApiSABLL>();
            services.AddSingleton<WagerRedoSABLL>();
            // AG
            services.AddSingleton<WagerAGBLL>();
            services.AddSingleton<ApiAGBLL>();
            services.AddSingleton<WagerRedoAGBLL>();
            services.AddSingleton<TicketAGBLL>();
            // CF
            services.AddSingleton<WagerCFBLL>();
            services.AddSingleton<ApiCFBLL>();
            // SG
            services.AddSingleton<WagerSGBLL>();
            services.AddSingleton<ApiSGBLL>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
}
