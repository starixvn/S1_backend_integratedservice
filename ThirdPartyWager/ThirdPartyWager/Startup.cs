using System.Net;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Workers;

namespace ThirdPartyWager
{
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddDatabaseConnection(configuration);

            if (!string.IsNullOrEmpty(configuration.GetValue<string>("ProxyHost")))
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
            services.AddSingleton<TpCustInfoService>();
            services.AddSingleton<CommonService>();
            services.AddSingleton<WagerSimplePlayService>();
            services.AddSingleton<WagerPlayTechService>();
            services.AddSingleton<LiveStreamingService>();
            services.AddSingleton<LatestHighlightVideoService>();
            services.AddSingleton<SabaOddsService>();
            services.AddSingleton<WagerMPService>();
            services.AddSingleton<WagerAPService>();
            services.AddSingleton<WagerYeebetService>();
            
            services.AddSingleton<IWagerSBOService, WagerSBOService>();
            services.AddSingleton<TpUnsettleService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<FincanceService>();
            services.AddSingleton<WagerTFGService>();
            services.AddSingleton<WagerIMService>();

            services.AddOptionListByDB<ServerInfo>("Admin_ServerInfoList_Get");
            services.AddOptionListByDB<dynamic>("Service_TpWagerTimer_Get", new { TpId = 1, Type = 1 }, DataBase.Wager);
            services.AddOptionListByDB<dynamic>("Tp_Get", new { TpId = 1 }, DataBase.Main);
            services.AddOptionListByDB<CurrencyOption>("Common_Currency_Get");

            //處理SiteConfig
            services.AddOptionListByDB<ConfigObject>("SiteConfig_Get", new { ConfigGroup = "TP" });
            services.AddSiteConfig();
            services.AddSingleton<ConfigBLL>();

            /*BLL*/
            services.AddSingleton<ThirdPartyBLL>();
            services.AddSingleton<CommonBLL>();
            services.AddSingleton<TpRefundBLL>();


            //SimplePlay
            services.AddSingleton<ApiSimplePlayBLL>();
            services.AddSingleton<WagerSimplePlayBLL>();
            services.AddSingleton<FinanceSimplePlayBLL>();

            //SBO
            services.AddSingleton<ISBOBLL, SBOBLL>();
            services.AddSingleton<IApiSBOBLL, ApiSBOBLL>();
            services.AddSingleton<FinanceSBOBLL>();
            
            // PlayTech
            services.AddSingleton<WagerPlayTechBLL>();
            services.AddSingleton<ApiPlayTechBLL>();
            services.AddSingleton<FinancePlayTechBLL>();
            // LiveStreaming
            services.AddSingleton<LiveStreamingBLL>();
            // LatestHighlightVideo
            services.AddSingleton<LatestHighlightVideoBLL>();
            
            //SabaOdds
            services.AddSingleton<SabaOddsBLL>();

            //MP
            services.AddSingleton<WagerMPBLL>();
            services.AddSingleton<ApiMPBLL>();
            services.AddSingleton<FinanceMPBLL>();

            //AP
            services.AddSingleton<ApiAPBLL>();
            services.AddSingleton<FinanceAPBLL>();
            services.AddSingleton<WagerAPBLL>();
            //Netent
            services.AddSingleton<ApiNetentBLL>();
            services.AddSingleton<FinanceNetentBLL>();
            //RedTiger
            services.AddSingleton<ApiRedTigerBLL>();
            services.AddSingleton<FinanceRedTigerBLL>();
            //Skywind
            services.AddSingleton<ApiSkywindBLL>();
            services.AddSingleton<WagerSkywindBLL>();
            //Yeebet
            services.AddSingleton<WagerYeebetBLL>();
            services.AddSingleton<ApiYeebetBLL>();
            services.AddSingleton<FinanceYeebetBLL>();

            //TFG
            services.AddSingleton<ApiTFGBLL>();
            services.AddSingleton<WagerTFGBLL>();

            //Nextspin
            services.AddSingleton<IWagerNextspinService, WagerNextspinService>();
            services.AddSingleton<INextspinApi, NextspinApi>();
            services.AddSingleton<INextspinBLL, NextspinBLL>();
            services.AddSingleton<INextspinFinancBLL, NextspinFinancBLL>();

            //IM
            services.AddSingleton<ApiIMBLL>();
            services.AddSingleton<WagerIMBLL>();
            services.AddSingleton<FinanceIMBLL>();
            services.AddSingleton<WagerSPRIBEBLL>();
            services.AddSingleton<FinanceSPRIBEBLL>();

            //Worker
            services.AddHostedService<WagerSimplePlay>();
            services.AddHostedService<WagerRedoSimplePlay>();
            services.AddHostedService<FinanceSimplePlay>();
            services.AddHostedService<WagerSBO>();
            services.AddHostedService<FinanceSBO>();

            services.AddHostedService<WagerPlayTech>();
            services.AddHostedService<WagerRedoPlayTech>();
            services.AddHostedService<FinancePlayTech>();
            services.AddHostedService<LiveStreaming>();
            services.AddHostedService<LatestHighlightVideo>();
            services.AddHostedService<SabaOdds>();
            
            services.AddHostedService<WagerMP>();
            services.AddHostedService<WagerRedoMP>();
            services.AddHostedService<FinanceMP>();
            services.AddHostedService<TpRefundWorker>();
  
            services.AddHostedService<FinanceAP>();
            services.AddHostedService<WagerAP>();
            services.AddHostedService<WagerRedoAP>();
            services.AddHostedService<FinanceNetent>();
            services.AddHostedService<FinanceRedTiger>();
            services.AddHostedService<WagerSkywind>();
            services.AddHostedService<WagerUnsettleCheckSkywind>();
            services.AddHostedService<WagerYeebet>();
            services.AddHostedService<FinanceYeebet>();
            
           
            services.AddHostedService<WagerTFG>();
            services.AddHostedService<WagerUnsettleCheckTFG>();
            services.AddHostedService<WagerNextspin>();
            services.AddHostedService<FinanceNextspin>();
            services.AddHostedService<WagerIM>();
            services.AddHostedService<WagerSPRIBE>();
            services.AddHostedService<FinanceIM>();
            services.AddHostedService<FinanceSPRIBE>();

            /*******************closed tp*************************/
            //services.AddHostedService<WagerAT>();
            //services.AddHostedService<WagerRedoAT>();
            //services.AddHostedService<FinanceAT>();

            services.AddTpList();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
}
