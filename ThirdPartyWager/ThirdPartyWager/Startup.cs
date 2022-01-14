using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.BLL.KM;
using ThirdPartyWager.Service.BLL.MG;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Workers;
using ThirdPartyWager.Workers.KM;
using ThirdPartyWager.Workers.MG;

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
            services.AddSingleton<CommonService>();
            services.AddSingleton<WagerDGService>();
            services.AddSingleton<WagerAllBetService>();
            services.AddSingleton<WagerSAService>();
            services.AddSingleton<WagerAGService>();
            //services.AddSingleton<WagerCFService>();
            services.AddSingleton<WagerSGService>();
            services.AddSingleton<WagerWMService>();
            services.AddSingleton<WagerKiss918Service>();
            services.AddSingleton<WagerOne789Service>();
            services.AddSingleton<WagerMegaService>();
            services.AddSingleton<WagerIconicService>();
            services.AddSingleton<WagerOneGamingService>();
            services.AddSingleton<WagerV8Service>();
            services.AddSingleton<WagerN2LiveService>();
            services.AddSingleton<WagerPTService>();
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LatestHighlightVideoService>();
            services.AddSingleton<SabaOddsService>();
            services.AddSingleton<WagerKMService>();
            services.AddSingleton<WagerMPService>();
            services.AddSingleton<WagerEVOService>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<CommonBLL>();
            services.AddSingleton<TpRefundBLL>();
            // DG
            services.AddSingleton<WagerDGBLL>();
            services.AddSingleton<ApiDGBLL>();
            services.AddSingleton<WagerRedoDGBLL>();
            // AllBet
            services.AddSingleton<WagerAllBetBLL>();
            services.AddSingleton<ApiAllBetBLL>();
            services.AddSingleton<WagerRedoAllBetBLL>();
            services.AddSingleton<RewardAllBetBLL>();
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
            //services.AddSingleton<WagerCFBLL>();
            //services.AddSingleton<ApiCFBLL>();
            // SG
            services.AddSingleton<WagerSGBLL>();
            services.AddSingleton<ApiSGBLL>();
            // WM
            services.AddSingleton<WagerWMBLL>();
            services.AddSingleton<ApiWMBLL>();
            services.AddSingleton<WagerRedoWMBLL>();
            // Kiss918
            services.AddSingleton<WagerKiss918BLL>();
            services.AddSingleton<ApiKiss918BLL>();
            services.AddSingleton<WagerRedoKiss918BLL>();
            // One789
            services.AddSingleton<WagerOne789BLL>();
            services.AddSingleton<ApiOne789BLL>();
            // Mega
            services.AddSingleton<WagerMegaBLL>();
            services.AddSingleton<ApiMegaBLL>();
            // Iconic
            services.AddSingleton<WagerIconicBLL>();
            services.AddSingleton<ApiIconicBLL>();
            // OneGaming
            services.AddSingleton<WagerOneGamingBLL>();
            services.AddSingleton<ApiOneGamingBLL>();
            // V8
            services.AddSingleton<WagerV8BLL>();
            services.AddSingleton<ApiV8BLL>();
            services.AddSingleton<WagerRedoV8BLL>();
            // N2Live
            services.AddSingleton<WagerN2LiveBLL>();
            services.AddSingleton<ApiN2LiveBLL>();
            // PT
            services.AddSingleton<WagerPTBLL>();
            services.AddSingleton<ApiPTBLL>();
            // LiveStreaming
            services.AddSingleton<LiveStreamingBLL>();
            // LatestHighlightVideo
            services.AddSingleton<LatestHighlightVideoBLL>();
            //9W
            services.AddSingleton<NineWBLL>();
            //SabaOdds
            services.AddSingleton<SabaOddsBLL>();
            //KM
            services.AddSingleton<ApiKMBLL>();
            services.AddSingleton<KMBLL>();
            //MP
            services.AddSingleton<WagerMPBLL>();
            services.AddSingleton<ApiMPBLL>();
            //MG
            services.AddTransient<IApiMGBLL, ApiMGBLL>();
            services.AddTransient<IApiSWBLL, ApiSWBLL>();
            services.AddTransient<IMGBLL, MGBLL>();
            //TCG
            services.AddSingleton<TCGBLL>();
            //EVO
            services.AddSingleton<WagerEVOBLL>();
            services.AddSingleton<ApiEVOBLL>();

            //Worker
            services.AddHostedService<WagerDG>();
            services.AddHostedService<WagerRedoDG>();
            services.AddHostedService<WagerAllBet>();
            services.AddHostedService<WagerRedoAllBet>();
            services.AddHostedService<CheckRewardAllBet>();
            services.AddHostedService<WagerSA>();
            services.AddHostedService<WagerRedoSA>();
            services.AddHostedService<WagerAG>();
            services.AddHostedService<WagerRedoAG>();
            services.AddHostedService<CheckRefundAG>();
            services.AddHostedService<WagerSG>();
            services.AddHostedService<WagerWM>();
            services.AddHostedService<WagerRedoWM>();
            services.AddHostedService<WagerOne789>();
            services.AddHostedService<JwtIconic>();
            services.AddHostedService<WagerIconic>();
            services.AddHostedService<WagerOneGaming>();
            services.AddHostedService<WagerRedoOneGaming>();
            services.AddHostedService<WagerV8>();
            services.AddHostedService<WagerRedoV8>();
            services.AddHostedService<WagerN2Live>();
            services.AddHostedService<WagerRedoN2Live>();
            services.AddHostedService<WagerPT>();
            services.AddHostedService<WagerRedoPT>();
            services.AddHostedService<LiveStreaming>();
            services.AddHostedService<LatestHighlightVideo>();
            services.AddHostedService<SabaOdds>();
            services.AddHostedService<WagerKM>();
            services.AddHostedService<WagerMG>();
            services.AddHostedService<WagerNineW>();
            services.AddHostedService<WagerTCG>();
            services.AddHostedService<WagerMP>();
            services.AddHostedService<WagerRedoMP>();
            services.AddHostedService<WagerEVO>();
            services.AddHostedService<WagerRedoEVO>();
            services.AddHostedService<TpRefundWorker>();

            /*
                services.AddHostedService<WagerCF>();
                services.AddHostedService<WagerKiss918>();
                services.AddHostedService<WagerRedoKiss918>();
                services.AddHostedService<WagerMega>();
                services.AddHostedService<WagerRedoMega>();
            */
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
