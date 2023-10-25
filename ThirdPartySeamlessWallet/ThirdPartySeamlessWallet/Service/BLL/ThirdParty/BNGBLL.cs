using System;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class BNGBLL : IBNGBLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly PartitionService _partitionService;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        public BNGBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        }
        public BNGResponse Transaction(TpCustInfo tpCustInfo, BNGRequest bettingData, out decimal afterAmount)
        {
            BNGResponse result = new BNGResponse();
            var tempResult = 0;
            //bettingData.TicketType = (int)BettransType.Bet;
            //bettingData.TicketStatus = bettingData.NetWin > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0;

            var partitionTime = DateTimeOffset.Parse(bettingData.c_at).DateTime;

            var check = _partitionService.Query<BettransRng_BNG>(partitionTime, "Service_BettransRng_BNG_Check", new { bettingData.uid, bettingData.args.round_id });
            if (check.Any())
            {
                return new BNGResponse()
                {
                    uid = bettingData.uid,
                    balance = new balance()
                    {
                        value = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance.ToString(),
                        version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            }
                };
            }
        }
    }
}
