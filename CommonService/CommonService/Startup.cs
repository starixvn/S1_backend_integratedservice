using CommonService.Service.BLL;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace CommonService
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
            services.Configure<SendXConfig>(Configuration.GetSection("SendXConfig"));
            services.Configure<Dictionary<string, SendGridConfig>>(Configuration.GetSection("SendGridConfig"));
            services.Configure<Dictionary<string, MoviderVerifySMSConfig>>(Configuration.GetSection("MoviderVerifySMSConfig"));
            services.Configure<Dictionary<string, TwilioVerifySMSServices>>(Configuration.GetSection("TwilioVerifySMSServices"));
            services.Configure<IovationServicesConfig>(Configuration.GetSection("IovationServicesConfig"));

            services.AddMemoryCache();
            HttpHelper.proxyHost = Configuration.GetValue<string>("ProxyHost");
            HttpHelper.proxyPort = Configuration.GetValue<int>("ProxyPort");

            services.AddSingleton<HttpHelper>();

            services.AddSingleton<CustInfoService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<IovationService>();
            services.AddSingleton<OTPSMSService>();
            

            services.AddSingleton<CustInfoBLL>();
            services.AddSingleton<AuthBLL>();
            services.AddSingleton<SendXBLL>();
            services.AddSingleton<SendGridBLL>();
            services.AddSingleton<RestPasswordBLL>();
            services.AddSingleton<MoviderSMSBLL>();
            services.AddSingleton<TwilioSMSBLL>();
            services.AddSingleton<IovationBLL>();
            services.AddSingleton<OTPSMSBLL>();
            

            services.AddControllers();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}");
                });

                endpoints.MapControllers();
            });
        }
    }
}
