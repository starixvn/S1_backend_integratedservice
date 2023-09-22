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
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        public FaChaiBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
        }

        public FCResponse Bet(TpCustInfo tpCustInfo, BettingDataRNG_FaChai bettingData, out decimal afterAmount)
        {
            FCResponse result = new FCResponse();
            var tempResult = 0;
            bettingData.TicketType = (int)BettransType.Bet;
            bettingData.TicketStatus = bettingData.NetWin > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0; 

            var partitionTime = ConvertLongToDateTime(bettingData.Ts);

            var check = _partitionService.Query<BettransRng_FaChai>(partitionTime, "Service_BettransRng_FaChai_Check", new { bettingData.BankID });
            if (check.Any())
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Duplicate_Transaction_ID_number
                };
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

            tempResult = _partitionService.Execute(partitionTime, "Service_BettransRng_FaChai_Ins", bettingData, trans);

            if (tempResult > 0 && bettingData.Bet != 0)
            {
                tempResult = _bettingService.PlaceBetV2(tpCustInfo, Math.Abs(bettingData.Bet) * -1, (int)Tp.FaChai, bettingData.RecordID, (int)GameType.FaChai_RNG, bettingData.RecordID, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }
            else
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Write_transaction_order_is_failed
                };
            }

            if (tempResult > 0 && bettingData.Win > 0)
            {
                tempResult = _settledService.PayoutV2(
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

            if (tempResult > 0)
            {
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.FaChai, GameType.FaChai_RNG, bettingData.BankID, trans);
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

                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Write_transaction_order_is_failed
                };
            }

            return new FCResponse()
            {
                Result = (int)FCErrorMessage.Success
            };
        }

        public FCResponse Cancel(TpCustInfo tpCustInfo, CnacelData_FaChai cancelData)
        {
            var result = new FCResponse();
            var partitionTime = ConvertLongToDateTime(cancelData.Ts);

            var check = _partitionService.Query<BettransRng_FaChai>(partitionTime, "Service_BettransRng_FaChai_Check", new { cancelData.BankID });
            if (check.Any())
            {
                result.Result = (int)FCErrorMessage.Revert_Cancel_Bet;
            }
            else
            {
                result.Result = (int)FCErrorMessage.Unknown_errors;
            }

            return result;
        }
        private DateTime ConvertLongToDateTime(long d)
        {
            DateTime dtStart = new DateTime(1970, 1, 1);
            long lTime = long.Parse(d + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            //轉成-4時區
            DateTime dtResult = dtStart.Add(toNow).AddHours(-4);
            return dtResult;
        }
    }
}
