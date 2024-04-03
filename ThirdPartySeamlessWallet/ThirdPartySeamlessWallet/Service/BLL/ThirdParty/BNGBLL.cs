using Newtonsoft.Json;
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
        private readonly ILogger<BNGBLL> _logger;
        public BNGBLL(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetService<ILogger<BNGBLL>>();
        }
        public int Transaction(TpCustInfo tpCustInfo, BNGRequest reqData, out decimal afterAmount)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0;

            var partitionTime = DateTimeOffset.Parse(reqData.c_at).DateTime;

            var check = _partitionService.Query<BettransRng_BNG>(partitionTime, "Service_BettransRng_BNG_Check", new { reqData.uid });
            if (check.Any())
            {
                afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance;
                return (int)TransactionStatus.Success;
            }
            var ticketType = reqData.args.win != null ? (int)BettransType.Payout : (int)BettransType.Bet;

            var bettingData = new BettransRng_BNG()
            {
                uid = reqData.uid,
                token = reqData.token,
                session = reqData.session,
                game_id = reqData.game_id,
                game_name = reqData.game_name,
                provider_id = reqData.provider_id,
                provider_name = reqData.provider_name,
                c_at = DateTimeOffset.Parse(reqData.c_at).DateTime,
                sent_at = DateTimeOffset.Parse(reqData.sent_at).DateTime,
                platform = string.IsNullOrEmpty(reqData.args.platform) ? string.Empty : reqData.args.platform,
                bet = reqData.args.bet != null ? Convert.ToDecimal(reqData.args.bet) : 0,
                win = reqData.args.win != null ? Convert.ToDecimal(reqData.args.win) : 0,
                round_started = reqData.args.round_started,
                round_finished = reqData.args.round_finished,
                round_id = reqData.args.round_id,
                id = reqData.args.player.id,
                brand = reqData.args.player.brand,
                currency = reqData.args.player.currency,
                mode = reqData.args.player.mode,
                is_test = reqData.args.player.is_test,
                bonus = reqData.args.bonus == null ? null : JsonConvert.SerializeObject(reqData.args.bonus),
                tag = reqData.args.tag,
                TicketType = ticketType
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.BNG, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.win *= tpCurrency.ExchangeRate;
                bettingData.bet *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_BettransRng_BNG_Ins", bettingData, trans);

            if (bettingData.bet > 0)
            {
                tempResult = _bettingService.PlaceBet(tpCustInfo, Math.Abs(bettingData.bet) * -1, (int)Tp.BNG, reqData.args.round_id.ToString(), (int)GameType.BNG_RNG, reqData.args.round_id.ToString(), out afterAmount, ref bonusId, out _, conn, trans);
            }

            bool getBonus = false;
            if (tempResult > 0 && bettingData.win > 0)
            {
                if (bonusId > 0)
                {
                    getBonus = true;
                    tempResult = _settledService.PayoutWithBonusId(
                                tpCustInfo,
                                bettingData.win,
                                (int)Tp.BNG,
                                reqData.args.round_id.ToString(),
                                (int)GameType.BNG_RNG,
                                TpCategory.Slots,
                                reqData.args.round_id.ToString(),
                                TransType.Payout,
                                out afterAmount,
                                "Payout",
                                bonusId,
                                conn,
                                trans
                            );
                }
                else
                {
                    tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        bettingData.win,
                        (int)Tp.BNG,
                        reqData.args.round_id.ToString(),
                        (int)GameType.BNG_RNG,
                        TpCategory.Slots,
                        reqData.args.round_id.ToString(),
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans);
                }
            }

            if (tempResult > 0)
            {
                if (reqData.args.round_finished)
                {
                    _ = _tpUnsettleService.InsertTpUnsettle(Tp.BNG, GameType.BNG_RNG, bettingData.round_id.ToString(), trans);
                }

                _mainDbClient.CommitAndDisposeConnection(conn, trans);

                if (getBonus)
                {
                    //call Service_TpSlotsBonus_Get to set flag
                    _settledService.GetTpBonus((int)Tp.BNG, bettingData.round_id.ToString(), (int)GameType.BNG_RNG, TpCategory.Slots, out int custId, out bonusId);
                }

                if (afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
                else if (afterAmount == 0)
                {
                    afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance;
                }
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);

                return (int)TransactionStatus.Fail;
            }

            return (int)TransactionStatus.Success;
        }

        public int Rollback(TpCustInfo tpCustInfo, BNGRequest reqData, out decimal afterAmount)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0;

            var partitionTime = DateTimeOffset.Parse(reqData.c_at).DateTime;

            var betCheck = _partitionService.Query<BettransRng_BNG>(partitionTime, "Service_BettransRng_BNG_Check", new { uid = reqData.args.transaction_uid });

            //檢查是否有這筆注單
            if (!betCheck.Any())
            {
                afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance;
                return (int)TransactionStatus.Success;
            }

            var rollBackCheck = _partitionService.Query<BettransRng_BNG>(partitionTime, "Service_BettransRng_BNG_Check", new { uid = reqData.uid });

            //檢查這個交易是否處理過
            if (rollBackCheck.Any())
            {
                afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance;
                return (int)TransactionStatus.Success;
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.BNG, tpCustInfo.CurrencyId);

            var bettingData = new BettransRng_BNG()
            {
                uid = reqData.uid,
                token = reqData.token,
                session = reqData.session,
                game_id = reqData.game_id,
                game_name = reqData.game_name,
                provider_id = reqData.provider_id,
                provider_name = reqData.provider_name,
                c_at = DateTimeOffset.Parse(reqData.c_at).DateTime,
                sent_at = DateTimeOffset.Parse(reqData.sent_at).DateTime,
                platform = string.IsNullOrEmpty(reqData.args.platform) ? string.Empty : reqData.args.platform,
                bet = (reqData.args.bet != null ? Convert.ToDecimal(reqData.args.bet) : 0) * -1,
                win = reqData.args.win != null ? Convert.ToDecimal(reqData.args.win) : 0,
                round_started = reqData.args.round_started,
                round_finished = reqData.args.round_finished,
                round_id = reqData.args.round_id,
                id = reqData.args.player.id,
                brand = reqData.args.player.brand,
                currency = reqData.args.player.currency,
                mode = reqData.args.player.mode,
                is_test = reqData.args.player.is_test,
                bonus = reqData.args.bonus == null ? null : JsonConvert.SerializeObject(reqData.args.bonus),
                tag = reqData.args.tag,
                TicketType = (int)BettransType.CancelBet
            };

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.win *= tpCurrency.ExchangeRate;
                bettingData.bet *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_BettransRng_BNG_Ins", bettingData, trans);

            if (tempResult > 0)
            {
                tempResult = _settledService.PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettingData.bet),
                        (int)Tp.BNG,
                        reqData.args.round_id.ToString(),
                        (int)GameType.BNG_RNG,
                        TpCategory.Slots,
                        reqData.args.round_id.ToString(),
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "Refund",
                        out bonusId,
                        conn,
                        trans
                    );
            }

            if (tempResult > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);

                if (afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
                else if (afterAmount == 0)
                {
                    afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpCustInfo.TpUserName, (int)GameType.BNG_RNG).Balance;
                }
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);

                return (int)TransactionStatus.Fail;
            }

            return (int)TransactionStatus.Success;
        }
    }
}
