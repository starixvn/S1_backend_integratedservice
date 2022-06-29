using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartyBetHistory.Extensions;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;

namespace ThirdPartyBetHistory
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDatabaseConnection(Configuration);
            services.AddMemoryCache();
            if (!string.IsNullOrEmpty(Configuration.GetValue<string>("ProxyHost")))
            {
                var proxy = new HttpClientHandler
                {
                    Proxy = new WebProxy(Configuration.GetValue<string>("ProxyHost"), Configuration.GetValue<int>("ProxyPort")),
                    UseProxy = true
                };

                services.AddHttpClient("with_proxy")
                .ConfigurePrimaryHttpMessageHandler(() => proxy);
            }
            else
            {
                services.AddHttpClient();
            }

            services.AddSingleton<HttpHelper>();

            /*DAL*/
            services.AddSingleton<SettledService>();
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<ConfigService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<SettledBLL>();
            services.AddSingleton<TpBetRecordExtendBLL>();

            services.RegisterSabaSiteHelper();

            services.AddOptionListByDB<TpGameTypeOption>("Admin_TpGameType_Get");
            services.AddOptionListByDB<TpListOption>("Common_TpList_Get");
        }

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
