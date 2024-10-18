using StackExchange.Redis;

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
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(Configuration.GetValue<string>("RedisConnection")));
            services.AddSingleton<ICacheHelper, RedisCacheHelper>();

            /*DAL*/
            services.AddSingleton<ThirdPartyService>();
            services.AddSingleton<ConfigService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();

            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.RegisterSabaSiteHelper();
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
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
                endpoints.MapGet("/healthcheck", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
                endpoints.MapGet("/health", async context =>
                {
                    await context.Response.WriteAsync($"{env.EnvironmentName}-{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
                });
            });
        }
    }
}