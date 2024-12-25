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
                //{ Tp.YL, AddYL},
                { Tp.YGG, AddYGG},
                { Tp.SG, AddSG},
                { Tp.KM, AddKM},
                { Tp.Allbet, AddAllbet},
                { Tp.OneGaming, AddOneGaming},
                { Tp.Sexy, AddSexy},
                { Tp.CQ9, AddCQ9},
                //{ Tp.CF, AddCF},
                { Tp.EBet, AddEBet},
                { Tp.WM, AddWM},
                //{ Tp.Kiss918, AddKiss918},
                //{ Tp.Mega, AddMega},
                { Tp.One789, AddOne789},
                //{ Tp.Iconic, AddIconic},
                { Tp.V8, AddV8},
                //{ Tp.N2Live, AddN2Live},
                { Tp.AMB, AddAMB},
                { Tp.NineW, AddNineW},
                { Tp.JILI, AddJILI},
                { Tp.TCG, AddTCG},
                { Tp.MG, AddMG},
                { Tp.MP, AddMP},
                { Tp.EVO, AddEVO},
                { Tp.Netent, AddNetent},
                { Tp.RedTiger, AddRedTiger},
                { Tp.AP, AddAP},
                { Tp.SBO, AddSBO},
                { Tp.Skywind, AddSkywind},
                { Tp.SimplePlay, AddSimplePlay},
                { Tp.Yeebet, AddYeebet},
                { Tp.BG, AddBG},
                { Tp.TFG, AddTFG},
                { Tp.Nextspin, AddNextspin},
                { Tp.PlayTech, AddPlayTech},
                { Tp.IM, AddIM},
                { Tp.SPRIBE, AddSPRIBE},
                { Tp.FaChai, AddFaChai},
                { Tp.Pegasus, AddPegasus},
                { Tp.BNG, AddBNG},
                { Tp.HaoLong, AddHL},
                { Tp.NLC, AddNLC},
                { Tp.MaxGame, AddMaxGame},
                { Tp.SabaPlay, AddSabaPlay},
                { Tp.UuSlots, AddUuSlots},
                { Tp.Live22, AddLive22},
                { Tp.FastSpin, AddFastSpin},
                { Tp.NTConnect, AddNTConnect },
                { Tp.GA, AddGA},
                { Tp.WonCasino, AddWonCasino},
                { Tp.DragonGaming, AddDragonGaming},
                { Tp.KY, AddKY },
            };
        }
        private IServiceCollection AddSA(IServiceCollection services)
        {
            services.AddSingleton<WagerSAService>();

            services.AddSingleton<WagerSABLL>();
            services.AddSingleton<ApiSABLL>();
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
            services.AddSingleton<FinanceAGBLL>();
            services.AddSingleton<TicketAGBLL>();

            services.AddHostedService<WagerAG>();
            services.AddHostedService<WagerRedoAG>();
            services.AddHostedService<FinanceAG>();
            services.AddHostedService<TicketAG>();

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
            //services.AddHostedService<WagerRedoDG>();
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
            services.AddSingleton<TicketOne789BLL>();

            services.AddHostedService<WagerOne789>();
            services.AddHostedService<FinanceOne789>();
            services.AddHostedService<TicketOne789>();

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

            //services.AddHostedService<WagerAT>();
            //services.AddHostedService<WagerRedoAT>();
            //services.AddHostedService<FinanceAT>();

            return services;
        }

        private IServiceCollection AddV8(IServiceCollection services)
        {
            services.AddSingleton<WagerV8Service>();

            services.AddSingleton<WagerV8BLL>();
            services.AddSingleton<ApiV8BLL>();
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
            services.AddSingleton<WagerAMBBLL>();
            services.AddSingleton<WagerAMBService>();

            services.AddHostedService<FinanceAMB>();
            services.AddHostedService<WagerAMB>();
            services.AddHostedService<WagerRedoAMB>();

            return services;
        }

        private IServiceCollection AddNineW(IServiceCollection services)
        {
            services.AddSingleton<NineWBLL>();
            services.AddHostedService<WagerNineW>();
            return services;
        }

        private IServiceCollection AddJILI(IServiceCollection services)
        {
            services.AddSingleton<ApiJILIBLL>();
            services.AddSingleton<FinanceJILIBLL>();

            services.AddHostedService<FinanceJILI>();
            return services;
        }

        private IServiceCollection AddTCG(IServiceCollection services)
        {
            services.AddSingleton<TCGBLL>();
            services.AddSingleton<FinanceTCGBLL>();

            services.AddHostedService<WagerTCG>();
            services.AddHostedService<FinanceTCG>();
            return services;
        }

        private IServiceCollection AddMG(IServiceCollection services)
        {
            services.AddTransient<IApiMGBLL, ApiMGBLL>();
            services.AddTransient<IApiSWBLL, ApiSWBLL>();
            services.AddTransient<IMGBLL, MGBLL>();

            services.AddHostedService<WagerMG>();
            services.AddHostedService<FinanceMG>();
            return services;
        }

        private IServiceCollection AddMP(IServiceCollection services)
        {
            services.AddSingleton<WagerMPService>();

            services.AddSingleton<WagerMPBLL>();
            services.AddSingleton<ApiMPBLL>();
            services.AddSingleton<FinanceMPBLL>();

            services.AddHostedService<WagerMP>();
            services.AddHostedService<WagerRedoMP>();
            services.AddHostedService<FinanceMP>();
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

        private IServiceCollection AddNetent(IServiceCollection services)
        {
            services.AddSingleton<ApiNetentBLL>();
            services.AddSingleton<FinanceNetentBLL>();

            services.AddHostedService<FinanceNetent>();

            return services;
        }

        private IServiceCollection AddRedTiger(IServiceCollection services)
        {
            services.AddSingleton<ApiRedTigerBLL>();
            services.AddSingleton<FinanceRedTigerBLL>();

            services.AddHostedService<FinanceRedTiger>();

            return services;
        }

        private IServiceCollection AddAP(IServiceCollection services)
        {
            services.AddSingleton<WagerAPService>();

            services.AddSingleton<ApiAPBLL>();
            services.AddSingleton<FinanceAPBLL>();
            services.AddSingleton<WagerAPBLL>();

            services.AddHostedService<FinanceAP>();
            services.AddHostedService<WagerAP>();
            services.AddHostedService<WagerRedoAP>();

            return services;
        }

        private IServiceCollection AddSBO(IServiceCollection services)
        {
            services.AddSingleton<IWagerSBOService, WagerSBOService>();

            services.AddSingleton<ISBOBLL, SBOBLL>();
            services.AddSingleton<IApiSBOBLL, ApiSBOBLL>();
            services.AddSingleton<FinanceSBOBLL>();

            services.AddHostedService<WagerSBO>();
            services.AddHostedService<FinanceSBO>();

            return services;
        }

        private IServiceCollection AddSkywind(IServiceCollection services)
        {
            services.AddSingleton<ApiSkywindBLL>();
            services.AddSingleton<WagerSkywindBLL>();

            services.AddHostedService<WagerSkywind>();
            services.AddHostedService<WagerUnsettleCheckSkywind>();

            return services;
        }

        private IServiceCollection AddSimplePlay(IServiceCollection services)
        {
            services.AddSingleton<WagerSimplePlayService>();

            services.AddSingleton<ApiSimplePlayBLL>();
            services.AddSingleton<WagerSimplePlayBLL>();
            services.AddSingleton<FinanceSimplePlayBLL>();

            services.AddHostedService<WagerSimplePlay>();
            services.AddHostedService<WagerRedoSimplePlay>();
            services.AddHostedService<FinanceSimplePlay>();

            return services;
        }

        private IServiceCollection AddYeebet(IServiceCollection services)
        {
            services.AddSingleton<WagerYeebetService>();

            services.AddSingleton<WagerYeebetBLL>();
            services.AddSingleton<ApiYeebetBLL>();
            services.AddSingleton<FinanceYeebetBLL>();

            services.AddHostedService<WagerYeebet>();
            services.AddHostedService<FinanceYeebet>();

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

        private IServiceCollection AddTFG(IServiceCollection services)
        {
            services.AddSingleton<WagerTFGService>();

            services.AddSingleton<ApiTFGBLL>();
            services.AddSingleton<WagerTFGBLL>();

            services.AddHostedService<WagerTFG>();
            services.AddHostedService<WagerUnsettleCheckTFG>();
            return services;
        }

        private IServiceCollection AddNextspin(IServiceCollection services)
        {
            services.AddSingleton<IWagerNextspinService, WagerNextspinService>();

            services.AddSingleton<INextspinApi, NextspinApi>();
            services.AddSingleton<INextspinBLL, NextspinBLL>();
            services.AddSingleton<INextspinFinancBLL, NextspinFinancBLL>();

            services.AddHostedService<WagerNextspin>();
            services.AddHostedService<FinanceNextspin>();
            return services;
        }

        private IServiceCollection AddPlayTech(IServiceCollection services)
        {
            services.AddSingleton<WagerPlayTechService>();

            services.AddSingleton<WagerPlayTechBLL>();
            services.AddSingleton<ApiPlayTechBLL>();
            services.AddSingleton<FinancePlayTechBLL>();

            services.AddHostedService<WagerPlayTech>();
            services.AddHostedService<WagerRedoPlayTech>();
            services.AddHostedService<FinancePlayTech>();

            return services;
        }

        private IServiceCollection AddIM(IServiceCollection services)
        {
            services.AddSingleton<WagerIMService>();

            services.AddSingleton<ApiIMBLL>();
            services.AddSingleton<WagerIMBLL>();
            services.AddSingleton<FinanceIMBLL>();

            services.AddHostedService<WagerIM>();
            services.AddHostedService<FinanceIM>();

            return services;
        }

        private IServiceCollection AddSPRIBE(IServiceCollection services)
        {
            services.AddSingleton<WagerIMService>();

            services.AddSingleton<WagerSPRIBEBLL>();
            services.AddSingleton<FinanceSPRIBEBLL>();

            services.AddHostedService<WagerSPRIBE>();
            services.AddHostedService<FinanceSPRIBE>();

            return services;
        }

        private IServiceCollection AddPegasus(IServiceCollection services)
        {
            services.AddSingleton<WagerPegasusService>();

            services.AddSingleton<ApiPegasusBLL>();
            services.AddSingleton<FinancePegasusBLL>();
            services.AddSingleton<WagerPegasusBLL>();

            services.AddHostedService<WagerPegasus>();
            services.AddHostedService<FinancePegasus>();
            return services;
        }

        private IServiceCollection AddFaChai(IServiceCollection services)
        {
            services.AddSingleton<WagerFaChaiService>();

            services.AddSingleton<ApiFaChaiBLL>();
            services.AddSingleton<FinanceFaChaiBLL>();
            services.AddSingleton<WagerFaChaiBLL>();

            services.AddHostedService<WagerFaChai>();
            services.AddHostedService<WagerRedoFaChai>();
            services.AddHostedService<FinanceFaChai>();

            return services;
        }

        private IServiceCollection AddBNG(IServiceCollection services)
        {
            services.AddSingleton<WagerBNGService>();

            services.AddSingleton<ApiBNGBLL>();
            services.AddSingleton<WagerBNGBLL>();
            services.AddSingleton<FinanceBNGBLL>();

            services.AddHostedService<WagerBNG>();
            services.AddHostedService<WagerRedoBNG>();
            services.AddHostedService<FinanceBNG>();

            return services;
        }

        private IServiceCollection AddHL(IServiceCollection services)
        {
            services.AddSingleton<WagerHLService>();

            services.AddSingleton<ApiHLBLL>();
            services.AddSingleton<FinanceHLBLL>();
            services.AddSingleton<WagerHLBLL>();

            services.AddHostedService<WagerHL>();
            services.AddHostedService<FinanceHL>();
            services.AddHostedService<WagerTicketUnSettleHL>();

            return services;
        }

        private IServiceCollection AddNLC(IServiceCollection services)
        {
            services.AddSingleton<WagerNLCService>();

            services.AddSingleton<WagerNLCBLL>();
            services.AddSingleton<ApiNLCBLL>();
            services.AddSingleton<FinanceNLCBLL>();

            services.AddHostedService<WagerNLC>();
            services.AddHostedService<WagerRedoNLC>();
            services.AddHostedService<WagerUnsettleNLC>();
            services.AddHostedService<FinanceNLC>();

            return services;
        }

        private IServiceCollection AddMaxGame(IServiceCollection services)
        {
            services.AddSingleton<WagerMaxGameService>();

            services.AddSingleton<ApiMaxGameBLL>();
            services.AddSingleton<TicketMaxGameBLL>();

            services.AddHostedService<TicketMaxGame>();

            return services;
        }
        private IServiceCollection AddSabaPlay(IServiceCollection services)
        {
            services.AddSingleton<WagerSabaPlayService>();

            services.AddSingleton<TicketSabaPlayBLL>();

            services.AddHostedService<TicketSabaPlay>();

            return services;
        }

        private IServiceCollection AddUuSlots(IServiceCollection services)
        {
            services.AddSingleton<WagerUuSlotsService>();

            services.AddSingleton<TicketUuSlotsBLL>();

            services.AddHostedService<TicketUuSlots>();

            return services;
        }

        private IServiceCollection AddLive22(IServiceCollection services)
        {
            services.AddSingleton<WagerLive22Service>();

            services.AddSingleton<TicketLive22BLL>();

            services.AddHostedService<TicketLive22>();

            return services;
        }

        private IServiceCollection AddFastSpin(IServiceCollection services)
        {
            services.AddSingleton<WagerFastSpinService>();

            services.AddSingleton<TicketFastSpinBLL>();

            services.AddHostedService<TicketFastSpin>();

            return services;
        }

        private IServiceCollection AddGA(IServiceCollection services)
        {
            services.AddSingleton<WagerGAService>();

            services.AddSingleton<ApiGABLL>();
            services.AddSingleton<WagerGABLL>();
            services.AddSingleton<FinanceGABLL>();

            services.AddHostedService<WagerGA>();
            services.AddHostedService<FinanceGA>();
            services.AddHostedService<WagerRedoGA>();

            return services;
        }

        private IServiceCollection AddNTConnect(IServiceCollection services)
        {
            services.AddSingleton<WagerNTConnectService>();

            services.AddSingleton<ApiNTConnectBLL>();
            services.AddSingleton<FinanceNTConnectBLL>();
            services.AddSingleton<WagerNTConnectBLL>();

            services.AddHostedService<WagerNTConnect>();
            services.AddHostedService<FinanceNTConnect>();

            return services;
        }

        private IServiceCollection AddWonCasino(IServiceCollection services)
        {
            services.AddSingleton<WagerWonCasinoService>();

            services.AddSingleton<WagerWonCasinoBLL>();
            services.AddSingleton<FinanceWonCasinoBLL>();
            services.AddSingleton<ApiWonCasinoBLL>();

            services.AddHostedService<WagerWonCasino>();
            services.AddHostedService<WagerRedoWonCasino>();
            services.AddHostedService<FinanceWonCasino>();

            return services;
        }

        private IServiceCollection AddDragonGaming(IServiceCollection services)
        {
            services.AddSingleton<WagerDragonGamingService>();

            services.AddSingleton<WagerDragonGamingBLL>();
            services.AddSingleton<FinanceDragonGamingBLL>();
            services.AddSingleton<ApiDragonGamingBLL>();

            services.AddHostedService<WagerDragonGaming>();
            services.AddHostedService<WagerRedoDragonGaming>();
            services.AddHostedService<FinanceDragonGaming>();

            return services;
        }

        private IServiceCollection AddKY(IServiceCollection services)
        {
            services.AddSingleton<WagerKYService>();

            services.AddSingleton<WagerKYBLL>();
            services.AddSingleton<ApiKYBLL>();
            services.AddSingleton<FinanceKYBLL>();

            services.AddHostedService<WagerKY>();
            services.AddHostedService<WagerRedoKY>();
            services.AddHostedService<FinanceKY>();

            return services;
        }
    }

}


