using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.DAL;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class FaChaiBLL : IFaChaiBLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly PartitionService _partitionService;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        public FaChaiBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        }

        public int Bet(TpCustInfo tpCustInfo, BettingDataRNG_FaChai bettingData, out decimal afterAmount)
        {
            bettingData.TicketType = (int)BettransType.Bet;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0; 

            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData.Ts).DateTime;

            var check = _partitionService.Query<BettransRng_FaChai>(partitionTime, "Service_BettransRng_FaChai_Check", new { bettingData.BankID });
            if (check.Any())
            {
                return -1;
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.FaChai, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.Bet *= tpCurrency.ExchangeRate;
                bettingData.Win *= tpCurrency.ExchangeRate;
                bettingData.JPBet *= tpCurrency.ExchangeRate;
                bettingData.JPPrize *= tpCurrency.ExchangeRate;
                bettingData.NetWin *= tpCurrency.ExchangeRate;
                bettingData.RequireAmt *= tpCurrency.ExchangeRate;
            }

            var result = 0;

            result = _partitionService.Execute(partitionTime, "Service_BettransRng_FaChai_Ins", bettingData, trans);

            if (result > 0 && bettingData.Bet != 0)
            {
                result = _bettingService.PlaceBetV2(tpCustInfo, Math.Abs(bettingData.Bet) * -1, (int)Tp.FaChai, bettingData.RecordID, (int)GameType.FaChai_RNG, bettingData.RecordID, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }
            else
            {
                return -1;
            }

            if (result > 0 && bettingData.Win > 0)
            {
                result = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.Win,
                        (int)Tp.FaChai,
                        bettingData.RecordID,
                        (int)GameType.FaChai_RNG,
                        TpCategory.Slots,
                        bettingData.RecordID,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans);
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);

                if (afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
                else if (afterAmount == 0)
                {
                    afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.Skywind, bettingData.MemberAccount, (int)GameType.FaChai_RNG).Balance;
                }
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return result;
        }

        public FCResponse Cancel(TpCustInfo tpCustInfo, CnacelData_FaChai cancelData)
        {
            var result = new FCResponse();
            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(cancelData.Ts).DateTime;

            var check = _partitionService.Query<BettransRng_FaChai>(partitionTime, "Service_BettransRng_FaChai_Check", new { cancelData.BankID });
            if (!check.Any())
            {
                result.Result = (int)FCErrorMessage.Transaction_ID_number_not_exist;
            }
            else
            {
                result.Result = (int)FCErrorMessage.Unknown_errors;
            }

            return result;
        }
    }
}
