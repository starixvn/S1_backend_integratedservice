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
        public int Transaction(TpCustInfo tpCustInfo, BNGRequest reqData, out decimal afterAmount)
        {
            var tempResult = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            int bonusId = 0;
            afterAmount = 0;

            var partitionTime = DateTimeOffset.Parse(reqData.c_at).DateTime;

            var check = _partitionService.Query<BettransRng_BNG>(partitionTime, "Service_BettransRng_BNG_Check", new { reqData.uid, reqData.args.round_id });
            if (check.Any())
            {
                return (int)TransactionStatus.Success;
            }

            var bettingData = new BettransRng_BNG()
            {
                uid = reqData.uid,
                token = reqData.token,
                session = reqData.session,
                game_id = reqData.game_id,
                game_name = reqData.game_name,
                provider_id = reqData.provider_id,
                provider_name = reqData.provider_name,
                c_at = reqData.c_at,
                sent_at = reqData.sent_at,
                platform = reqData.args.platform,
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
                campaign = string.IsNullOrWhiteSpace(reqData.args.bonus.campaign) ? String.Empty : reqData.args.bonus.campaign,
                source = reqData.args.bonus.source,
                bonus_id = reqData.args.bonus.bonus_id,
                ext_bonus_id = string.IsNullOrWhiteSpace(reqData.args.bonus.ext_bonus_id) ? String.Empty : reqData.args.bonus.ext_bonus_id,
                bonus_type = string.IsNullOrWhiteSpace(reqData.args.bonus.bonus_type) ? String.Empty : reqData.args.bonus.bonus_type,
                Event = reqData.args.bonus.Event,
                start_date = string.IsNullOrWhiteSpace(reqData.args.bonus.start_date) ? String.Empty : reqData.args.bonus.start_date,
                end_date = string.IsNullOrWhiteSpace(reqData.args.bonus.end_date) ? String.Empty : reqData.args.bonus.end_date,
                total_bet = reqData.args.bonus.total_bet != null ? Convert.ToDecimal(reqData.args.bonus.total_bet) : 0,
                total_win = reqData.args.bonus.total_win != null ? Convert.ToDecimal(reqData.args.bonus.total_win) : 0,
                played_bet = reqData.args.bonus.played_bet != null ? Convert.ToDecimal(reqData.args.bonus.played_bet) : 0,
                played_win = reqData.args.bonus.played_win != null ? Convert.ToDecimal(reqData.args.bonus.played_win) : 0,
                status = string.IsNullOrWhiteSpace(reqData.args.bonus.status) ? String.Empty : reqData.args.bonus.status,
                tag = reqData.args.tag
            };

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.BNG, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData.win *= tpCurrency.ExchangeRate;
                bettingData.bet *= tpCurrency.ExchangeRate;
                bettingData.total_win *= tpCurrency.ExchangeRate;
                bettingData.total_bet *= tpCurrency.ExchangeRate;
                bettingData.played_win *= tpCurrency.ExchangeRate;
                bettingData.played_bet *= tpCurrency.ExchangeRate;
            }

            tempResult = _partitionService.Execute(partitionTime, "Service_BettransRng_BNG_Ins", bettingData, trans);

            if (reqData.args.round_started && bettingData.bet > 0)
            {
                tempResult = _bettingService.PlaceBetV2(tpCustInfo, Math.Abs(bettingData.bet) * -1, (int)Tp.BNG, reqData.args.round_id.ToString(), (int)GameType.BNG_RNG, reqData.args.round_id.ToString(), null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
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
                _ = _tpUnsettleService.InsertTpUnsettle(Tp.BNG, GameType.BNG_RNG, bettingData.round_id.ToString(), trans);
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
    }
}
