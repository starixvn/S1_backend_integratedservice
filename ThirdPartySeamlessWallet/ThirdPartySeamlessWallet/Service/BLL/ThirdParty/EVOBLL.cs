using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class EVOBLL : IEVOBLL, IThirdPartyBLL
    {
        #region Fields

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<EVOBLL> _logger;

        private readonly MySqlClient _mainDbClient;

        //private readonly BettingService _bettingService;
        //private readonly SettledService _settledService;

        #endregion

        #region Constructors

        public EVOBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.AMB;
            GameType = GameType.AMB_RNG;

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<EVOBLL>>();

            //_bettingService = provider.GetService<BettingService>();
            //_settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        #region Methods



        #endregion

    }
}
