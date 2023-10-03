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
                { Tp.BG, AddBG},
                { Tp.NineW, AddNineW},
            };
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

        private IServiceCollection AddNineW(IServiceCollection services)
        {
            services.AddSingleton<NineWBLL>();

            services.AddHostedService<WagerNineW>();
            return services;
        }
    }

}


