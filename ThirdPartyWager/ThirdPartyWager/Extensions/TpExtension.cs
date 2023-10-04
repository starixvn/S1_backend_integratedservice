using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Workers;

namespace ThirdPartyWager.Extensions
{
    public class TpExtension
    {
        public Dictionary<Tp, Func<IServiceCollection, IServiceCollection>> TpFuncDictionary;

        public TpExtension()
        {
            TpFuncDictionary = new Dictionary<Tp, Func<IServiceCollection, IServiceCollection>>
            {
                { Tp.SA, AddSA},
                { Tp.AG, AddAG},
                { Tp.DG, AddDG},
                { Tp.GPI, AddGPI},
                { Tp.YL, AddYL},
                { Tp.YGG, AddYGG},
                { Tp.SG, AddSG},
                { Tp.KM, AddKM},
                { Tp.Allbet, AddAllbet},
                { Tp.OneGaming, AddOneGaming},
                { Tp.Sexy, AddSexy},
                { Tp.CQ9, AddCQ9},
                { Tp.CF, AddCF},
                { Tp.EBet, AddEBet},
                { Tp.WM, AddWM},
                { Tp.Kiss918, AddKiss918},
                { Tp.Mega, AddMega},
                { Tp.One789, AddOne789},
                { Tp.Iconic, AddIconic},
                { Tp.V8, AddV8},
                { Tp.N2Live, AddN2Live},
                { Tp.AMB, AddAMB},
                { Tp.NineW, AddNineW},
                { Tp.EVO, AddEVO},
                { Tp.BG, AddBG}
            };
        }
        private IServiceCollection AddSA(IServiceCollection services)
        {
            services.AddSingleton<WagerSAService>();

            services.AddSingleton<WagerSABLL>();
            services.AddSingleton<ApiSABLL>();
            services.AddSingleton<WagerRedoSABLL>();
            services.AddSingleton<FinanceSABLL>();

            services.AddHostedService<WagerSA>();
            services.AddHostedService<WagerRedoSA>();
            services.AddHostedService<FinanceSA>();

            return services;
        }

        private IServiceCollection AddAG(IServiceCollection services)
        {
            services.AddSingleton<WagerAGService>();

            services.AddSingleton<WagerAGBLL>();
            services.AddSingleton<ApiAGBLL>();
            services.AddSingleton<WagerRedoAGBLL>();
            services.AddSingleton<FinanceAGBLL>();

            services.AddHostedService<WagerAG>();
            services.AddHostedService<WagerRedoAG>();
            services.AddHostedService<FinanceAG>();

            return services;
        }

        private IServiceCollection AddDG(IServiceCollection services)
        {
            services.AddSingleton<WagerDGService>();

            services.AddSingleton<WagerDGBLL>();
            services.AddSingleton<ApiDGBLL>();
            services.AddSingleton<WagerRedoDGBLL>();
            services.AddSingleton<FinanceDGBLL>();

            services.AddHostedService<WagerDG>();
            services.AddHostedService<WagerRedoDG>();
            services.AddHostedService<FinanceDG>();

            return services;
        }

        private IServiceCollection AddGPI(IServiceCollection services)
        {
            services.AddSingleton<WagerGPIService>();

            services.AddSingleton<WagerGPIBLL>();
            services.AddSingleton<ApiGPIBLL>();
            services.AddSingleton<FinanceGPIBLL>();

            services.AddHostedService<WagerGPI>();
            services.AddHostedService<WagerRedoGPI>();
            services.AddHostedService<FinanceGPI>();

            return services;
        }

        private IServiceCollection AddYL(IServiceCollection services)
        {
            services.AddSingleton<ApiYLBLL>();
            services.AddSingleton<FinanceYLBLL>();

            services.AddHostedService<FinanceYL>();

            return services;
        }

        private IServiceCollection AddYGG(IServiceCollection services)
        {

            services.AddSingleton<WagerYGGBLL>();
            services.AddSingleton<FinanceYGGBLL>();

            services.AddHostedService<WagerYGG>();
            services.AddHostedService<FinanceYGG>();

            return services;
        }

        private IServiceCollection AddSG(IServiceCollection services)
        {
            services.AddSingleton<WagerSGService>();

            services.AddSingleton<WagerSGBLL>();
            services.AddSingleton<ApiSGBLL>();
            services.AddSingleton<FinanceSGBLL>();

            services.AddHostedService<WagerSG>();
            services.AddHostedService<FinanceSG>();

            return services;
        }

        private IServiceCollection AddKM(IServiceCollection services)
        {
            services.AddSingleton<WagerKMService>();

            services.AddSingleton<ApiKMBLL>();
            services.AddSingleton<KMBLL>();
            services.AddSingleton<FinanceKMBLL>();

            services.AddHostedService<WagerKM>();
            services.AddHostedService<FinanceKM>();

            return services;
        }

        private IServiceCollection AddAllbet(IServiceCollection services)
        {
            services.AddSingleton<WagerAllBetService>();

            services.AddSingleton<WagerAllBetBLL>();
            services.AddSingleton<ApiAllBetBLL>();
            services.AddSingleton<WagerRedoAllBetBLL>();
            services.AddSingleton<RewardAllBetBLL>();
            services.AddSingleton<FinanceAllBetBLL>();

            services.AddHostedService<WagerAllBet>();
            services.AddHostedService<WagerRedoAllBet>();
            services.AddHostedService<FinanceAllBet>();
            services.AddHostedService<CheckRewardAllBet>();
            services.AddHostedService<WagerUnsettleCheckAllBet>();

            return services;
        }

        //closed tp
        private IServiceCollection AddOneGaming(IServiceCollection services)
        {
            //services.AddSingleton<WagerOneGamingService>();

            //services.AddSingleton<WagerOneGamingBLL>();
            //services.AddSingleton<ApiOneGamingBLL>();
            //services.AddSingleton<FinanceOneGamingBLL>();

            //services.AddHostedService<WagerOneGaming>();
            //services.AddHostedService<WagerRedoOneGaming>();
            //services.AddHostedService<FinanceOneGaming>();

            return services;
        }

        private IServiceCollection AddSexy(IServiceCollection services)
        {
            services.AddSingleton<ApiSexyBLL>();
            services.AddSingleton<FinanceSexyBLL>();

            services.AddHostedService<FinanceSexy>();

            return services;
        }

        private IServiceCollection AddCQ9(IServiceCollection services)
        {
            services.AddSingleton<ApiCQ9BLL>();
            services.AddSingleton<FinanceCQ9BLL>();

            services.AddHostedService<FinanceCQ9>();

            return services;
        }

        //closed tp
        private IServiceCollection AddCF(IServiceCollection services)
        {
            //services.AddSingleton<WagerCFService>();

            //services.AddSingleton<WagerCFBLL>();
            //services.AddSingleton<ApiCFBLL>();

            return services;
        }

        private IServiceCollection AddEBet(IServiceCollection services)
        {
            services.AddSingleton<ApiEbetBLL>();
            services.AddSingleton<FinanceEbetBLL>();

            services.AddHostedService<FinanceEbet>();

            return services;
        }

        private IServiceCollection AddWM(IServiceCollection services)
        {
            services.AddSingleton<WagerWMService>();

            services.AddSingleton<WagerWMBLL>();
            services.AddSingleton<ApiWMBLL>();
            services.AddSingleton<WagerRedoWMBLL>();
            services.AddSingleton<FinanceWMBLL>();

            services.AddHostedService<WagerWM>();
            services.AddHostedService<WagerRedoWM>();
            services.AddHostedService<FinanceWM>();

            return services;
        }

        //closed tp
        private IServiceCollection AddKiss918(IServiceCollection services)
        {
            //services.AddSingleton<WagerKiss918Service>();

            //services.AddSingleton<WagerKiss918BLL>();
            //services.AddSingleton<ApiKiss918BLL>();

            //services.AddHostedService<WagerKiss918>();
            //services.AddHostedService<WagerRedoKiss918>();

            return services;
        }

        //closed tp
        private IServiceCollection AddMega(IServiceCollection services)
        {
            //services.AddSingleton<WagerMegaService>();

            //services.AddSingleton<WagerMegaBLL>();
            //services.AddSingleton<ApiMegaBLL>();

            //services.AddHostedService<ReportMega>();
            //services.AddHostedService<ReportRedoMega>();

            return services;
        }

        private IServiceCollection AddOne789(IServiceCollection services)
        {
            services.AddSingleton<WagerOne789Service>();

            services.AddSingleton<WagerOne789BLL>();
            services.AddSingleton<ApiOne789BLL>();
            services.AddSingleton<FinanceOne789BLL>();

            services.AddHostedService<WagerOne789>();
            services.AddHostedService<FinanceOne789>();

            return services;
        }

        //closed tp
        private IServiceCollection AddIconic(IServiceCollection services)
        {
            //services.AddSingleton<WagerIconicService>();

            //services.AddSingleton<WagerIconicBLL>();
            //services.AddSingleton<ApiIconicBLL>();
            //services.AddSingleton<FinanceATBLL>();

            //services.AddHostedService<JwtIconic>();

            return services;
        }

        private IServiceCollection AddV8(IServiceCollection services)
        {
            services.AddSingleton<WagerV8Service>();

            services.AddSingleton<WagerV8BLL>();
            services.AddSingleton<ApiV8BLL>();
            services.AddSingleton<WagerRedoV8BLL>();
            services.AddSingleton<FinanceV8BLL>();

            services.AddHostedService<WagerV8>();
            services.AddHostedService<WagerRedoV8>();
            services.AddHostedService<FinanceV8>();

            return services;
        }

        private IServiceCollection AddN2Live(IServiceCollection services)
        {
            services.AddSingleton<WagerN2LiveService>();

            services.AddSingleton<WagerN2LiveBLL>();
            services.AddSingleton<ApiN2LiveBLL>();
            services.AddSingleton<FinanceN2LiveBLL>();

            services.AddHostedService<WagerN2Live>();
            services.AddHostedService<WagerRedoN2Live>();
            services.AddHostedService<FinanceN2Live>();

            return services;
        }

        private IServiceCollection AddAMB(IServiceCollection services)
        {
            services.AddSingleton<ApiAMBBLL>();
            services.AddSingleton<FinanceAMBBLL>();

            services.AddHostedService<FinanceAMB>();

            return services;
        }


        private IServiceCollection AddNineW(IServiceCollection services)
        {
            services.AddSingleton<NineWBLL>();
            services.AddHostedService<WagerNineW>();
            return services;
        }

        private IServiceCollection AddEVO(IServiceCollection services)
        {
            services.AddSingleton<WagerEVOService>();

            services.AddSingleton<WagerEVOBLL>();
            services.AddSingleton<ApiEVOBLL>();
            services.AddSingleton<FinanceEVOBLL>();

            services.AddHostedService<WagerEVO>();
            services.AddHostedService<WagerRedoEVO>();
            services.AddHostedService<WagerUnsettleEVO>();
            services.AddHostedService<FinanceEVO>();

            return services;
        }

        private IServiceCollection AddBG(IServiceCollection services)
        {
            services.AddSingleton<WagerBGService>();

            services.AddSingleton<CommonBGBLL>();
            services.AddSingleton<WagerBGBLL>();
            services.AddSingleton<ApiBGBLL>();
            services.AddSingleton<FinanceBGBLL>();

            services.AddHostedService<WagerBG>();
            services.AddHostedService<WagerRedoBG>();
            services.AddHostedService<FinanceBG>();
            return services;
        }
    }

}


