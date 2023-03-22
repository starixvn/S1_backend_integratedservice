using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TFGBLL : IThirdPartyBLL
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public TFGBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.TFG;
            GameType = GameType.TFG_Esports;
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
        }
    }
}
