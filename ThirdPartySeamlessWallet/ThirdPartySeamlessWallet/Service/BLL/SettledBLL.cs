using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class SettledBLL
    {
        private readonly SettledService settledService;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public SettledBLL(IServiceProvider provider)
        {
            settledService = provider.GetRequiredService<SettledService>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public int Settled(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, TpCategory tpCategory, GetTpSeamlessBonusType getTpSeamlessBonusType = GetTpSeamlessBonusType.ByGameTypeAndTransId)
        {
            var result = settledService.Settle(tpCustInfo, betRecordInfo, getTpSeamlessBonusType);
            return result;
        }

        public SettlementType GetSettlementType(GameType gameType, string gameId)
        {
            return settledService.GetSettlementType(gameType, gameId);
        }

        public int DoTpRewardTicket(TpCustInfo tpCustInfo, TpRewardRecordInfo tpRewardRecordInfo, TpCategory tpCategory, bool checkBonus = true)
        {
            var result = settledService.DoTpRewardTicket(tpCustInfo, tpRewardRecordInfo, tpCategory, checkBonus);
            return result;
        }

        public TpBetRecordDetail GetTpBetRecordDetail(int TpId, int GameType, string TransId, DateTime SettledTime)
        {
            return settledService.GetTpBetRecordDetail(TpId, GameType, TransId, SettledTime);
        }

        public int SetSettleFlagOfMonthlyBettrans(string spName, BetRecordInfo betRecordInfo)
        {
            return settledService.SetSettleFlagOfMonthlyBettrans(spName, betRecordInfo);
        }

        #region SA

        public int CancelBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_SA(tpCustInfo, bettingDataLC_SA, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PayoutLC_SA(PayoutDataLC_SA payoutDataLC_SA, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SA, payoutDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_SA(tpCustInfo, payoutDataLC_SA, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public SettleData_SA GetSettle_BettransLC_SA(string Gameid, string Username)
        {
            return settledService.GetSettle_BettransLC_SA(Gameid, Username);
        }

        #endregion

        #region DG

        public int CancelBetLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PayoutLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<BettingDataLC_DG> GetUnsettledListLC_DG()
        {
            return settledService.GetUnsettledListLC_DG();
        }
        public int Get_TpWagerDG_Settle(string TransId, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            return settledService.Get_TpWagerDG_Settle(TransId, out stake, out validBet, out winLostAmount, out settleTime);
        }

        public SettleData_DG GetSettle_TpWagerDG(string TransId)
        {
            return settledService.GetSettle_TpWagerDG(TransId);
        }

        public int SetSettleFlag_DG(string TransId)
        {
            return settledService.SetSettleFlag_DG(TransId);
        }
        public int SetSettleFlag_TpWagerDG(string transId)
        {
            return settledService.SetSettleFlag_TpWagerDG(transId);
        }

        #endregion

        #region AG

        public int PayoutLC_AG(PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_AG.NetAmount *= tpCurrency.ExchangeRate;
                payoutDataLC_AG.ValidBetAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_AG(tpCustInfo, payoutDataLC_AG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<DataLC_AG> GetUnsettledListLC_AG()
        {
            return settledService.GetUnsettledListLC_AG();
        }
        public int RefundLC_AG(BettingDataLC_AG_Refund bettingDataLC_AG_Refund, out decimal afterAmount)
        {
            string username = bettingDataLC_AG_Refund.Playname.Substring(bettingDataLC_AG_Refund.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_AG_Refund.Value *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundLC_AG(tpCustInfo, bettingDataLC_AG_Refund, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<SettleDataLC_AG> Get_PayoutList_AG(string transactionID)
        {
            return settledService.Get_PayoutList_AG(transactionID);
        }
        public IEnumerable<BettingDataLC_AG> Get_BetList_AG(string transactionIDs)
        {
            return settledService.Get_BetList_AG(transactionIDs);
        }
        public int SetSettleFlag_AG(string transactionID, string transactionIDs)
        {
            return settledService.SetSettleFlag_AG(transactionID, transactionIDs);
        }
        public int SetSettleFlag_TpWagerAG(string billNoList)
        {
            return settledService.SetSettleFlag_TpWagerAG(billNoList);
        }
        public int Get_TpWagerAG_Settle(string billNoList, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            var result = settledService.Get_TpWagerAG_Settle(billNoList, out stake, out validBet, out winLostAmount, out settleTime);
            return result;
        }

        public SettleData GetSettle_TpWagerAG(string TransId)
        {
            return settledService.GetSettle_TpWagerAG(TransId);
        }

        #endregion

        #region GPI

        public int PayoutRNG_GPI(SettledDataRNG_GPI settledDataRNG_GPI, out decimal afterAmount, out string TransId)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, settledDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settledDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
                if ((settledDataRNG_GPI.jackpot_win ?? 0m) != 0m)
                {
                    settledDataRNG_GPI.jackpot_win *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutRNG_GPI(tpCustInfo, settledDataRNG_GPI, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public IEnumerable<SettleDataRNG_GPI> GetUnsettledListRNG_GPI(string transId)
        {
            return settledService.GetUnsettledListRNG_GPI(transId);
        }

        public int SetSettleFlagRNG_GPI(string roundId, string custId, int SettleFlag)
        {
            return settledService.SetSettleFlagRNG_GPI(roundId, custId, SettleFlag);
        }

        #endregion

        #region YL

        public int SettledFish_YL(BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.YL, bettingDataFish_YL.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YL, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance((int)Tp.YL, bettingDataFish_YL.userId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_YL.betAmount *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.payAmount *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.profit *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.validBet *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.commission *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_YL(tpCustInfo, custBalance, tpCurrency, bettingDataFish_YL, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return -2;
            }

            var checkBalance = custBalance.Balance - bettingDataFish_YL.betAmount;
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_YL(tpCustInfo, custBalance, tpCurrency, bettingDataFish_YL, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return -2;
            }
            var result = settledService.SettledFish_YL(tpCustInfo, bettingDataFish_YL, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int RefundFish_YL(RefundDataFish_YL refundDataFish_YL, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.YL, refundDataFish_YL.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YL, tpCustInfo.CurrencyId);

            var result = settledService.RefundFish_YL(tpCustInfo, refundDataFish_YL, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public IEnumerable<SettleData> GetUnsettledListFish_YL()
        {
            return settledService.GetUnsettledListFish_YL();
        }

        public int SetSettleFlagFish_YL(string TransId)
        {
            return settledService.SetSettleFlagFish_YL(TransId);
        }


        #endregion

        #region Joker

        public int SettledRNG_Joker(BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, bettingDataRNG_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Joker.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_Joker(tpCustInfo, bettingDataRNG_Joker, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_Joker.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetRNG_Joker(CancelBetDataRNG_Joker cancelBetDataRNG_Joker, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, cancelBetDataRNG_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetRNG_Joker(tpCustInfo, cancelBetDataRNG_Joker, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int DepositFish_Joker(BettingDataFish_Joker bettingDataFish_Joker, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, bettingDataFish_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Joker.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.DepositFish_Joker(tpCustInfo, bettingDataFish_Joker, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataFish_Joker.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int TransactionFish_Joker(BettingDataFish_Joker bettingDataFish_Joker, out decimal afterAmount)
        {
            afterAmount = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Joker, bettingDataFish_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Joker.amount *= tpCurrency.ExchangeRate;
                bettingDataFish_Joker.result *= tpCurrency.ExchangeRate;
                bettingDataFish_Joker.startbalance *= tpCurrency.ExchangeRate;
                bettingDataFish_Joker.endbalance *= tpCurrency.ExchangeRate;
            }

            var result = settledService.TransactionFish_Joker(tpCustInfo, bettingDataFish_Joker);

            if (result > 0)
            {
                afterAmount = tpCustInfoBLL.GetBalance((int)Tp.Joker, bettingDataFish_Joker.username, (int)GameType.Joker_Fish).Balance;
            }

            return result;
        }

        #endregion

        #region YGG

        public int PayoutRNG_YGG(PayoutDataRNG_YGG payoutDataRNG_YGG, out decimal afterAmount, out string countryCode, SettleFlag settleFlag)
        {
            var result = 0;
            afterAmount = 0;
            countryCode = string.Empty;

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.YGG, payoutDataRNG_YGG.playerid);

            if (tpCustInfo != null)
            {
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);
                var tpCountry = tpCustInfoBLL.GetTpCurrencyCodeByCurrencyId((int)Tp.YGG, tpCustInfo.TpCurrencyId);

                if (tpCountry != null)
                {
                    countryCode = tpCountry.CountryCode;
                }

                var insertData = new PayoutDataRNG_YGG()
                {
                    playerid = payoutDataRNG_YGG.playerid,
                    amount = tpCurrency.ExchangeRate > 0 ? payoutDataRNG_YGG.amount * tpCurrency.ExchangeRate : payoutDataRNG_YGG.amount,
                    currency = payoutDataRNG_YGG.currency,
                    reference = payoutDataRNG_YGG.reference,
                    subreference = payoutDataRNG_YGG.subreference,
                    description = payoutDataRNG_YGG.description,
                    cat1 = payoutDataRNG_YGG.cat1,
                    cat2 = payoutDataRNG_YGG.cat2,
                    cat3 = payoutDataRNG_YGG.cat3,
                    cat4 = payoutDataRNG_YGG.cat4,
                    cat5 = payoutDataRNG_YGG.cat5,
                    tag1 = payoutDataRNG_YGG.tag1,
                    tag2 = payoutDataRNG_YGG.tag2,
                    tag3 = payoutDataRNG_YGG.tag3,
                    TicketStatus = payoutDataRNG_YGG.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                    SettleFlag = (int)settleFlag,
                    bonusprize = tpCurrency.ExchangeRate > 0 ? payoutDataRNG_YGG.bonusprize * tpCurrency.ExchangeRate : payoutDataRNG_YGG.bonusprize,
                    tickets = payoutDataRNG_YGG.tickets
                };

                result = settledService.PayoutRNG_YGG(tpCustInfo, insertData, out afterAmount);

                if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
            }

            return result;
        }

        public int CancelBetRNG_YGG(BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            int result = 0;
            afterAmount = 0;

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.YGG, bettingDataRNG_YGG.playerid);

            if (tpCustInfo != null)
            {
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);

                result = settledService.CancelBetRNG_YGG(tpCustInfo, bettingDataRNG_YGG, out afterAmount);
                if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;
                }
            }

            return result;
        }

        #endregion

        #region PP

        public int PayoutRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
                bettingDataRNG_PP.promoWinAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_PP(tpCustInfo, bettingDataRNG_PP, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int BonusWinRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.BonusWinRNG_PP(bettingDataRNG_PP, out afterAmount);

            return result;
        }
        public int CancelBetRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_PP(tpCustInfo, bettingDataRNG_PP, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int AdjustmentRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.AdjustmentRNG_PP(tpCustInfo, bettingDataRNG_PP, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PromoBonus_PP(PromoBonus promoBonus, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PP, promoBonus.UserId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                promoBonus.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PromoBonus_PP(tpCustInfo, promoBonus, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                promoBonus.Amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        #region SG

        public int CancelBetRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_SG(tpCustInfo, bettingDataRNG_SG, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PayoutRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_SG(tpCustInfo, bettingDataRNG_SG, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_SG.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public SettleData_SG GetSettle_TpWagerRng_SG(string TransId)
        {
            return settledService.GetSettle_TpWagerRng_SG(TransId);
        }

        #endregion

        #region PG

        public int SettledRNG_PG(BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PG, bettingDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_PG(tpCustInfo, bettingDataRNG_PG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_PG.TransferAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RewardRNG_PG(AdjustmentDataRNG_PG adjustmentDataRNG_PG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PG, adjustmentDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                adjustmentDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RewardRNG_PG(tpCustInfo, adjustmentDataRNG_PG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                adjustmentDataRNG_PG.TransferAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int BetAndPayoutRNG_PG(TransferInOutDataRNG_PG transferInOutDataRNG_PG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PG, transferInOutDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance((int)Tp.PG, transferInOutDataRNG_PG.PlayerName, (int)GameType.PG_RNG);

            if (tpCurrency.ExchangeRate > 0)
            {
                transferInOutDataRNG_PG.BetAmount *= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.WinAmount *= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.BonusBalanceAmount *= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.BonusRatioAmount *= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.JackpotRtpContributionAmount *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_PG(tpCustInfo, custBalance, tpCurrency, transferInOutDataRNG_PG, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(transferInOutDataRNG_PG.BetAmount);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_PG(tpCustInfo, custBalance, tpCurrency, transferInOutDataRNG_PG, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var result = settledService.BetAndPayoutRNG_PG(tpCustInfo, transferInOutDataRNG_PG, thirdPartyBLL, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.BetAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.WinAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.TransferAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.BonusBalanceAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.BonusRatioAmount /= tpCurrency.ExchangeRate;
                transferInOutDataRNG_PG.JackpotRtpContributionAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int ResettleRNG_PG(ResettleDataRNG_PG resettleDataRNG_PG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.PG, resettleDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                resettleDataRNG_PG.BetAmount *= tpCurrency.ExchangeRate;
                resettleDataRNG_PG.WinAmount *= tpCurrency.ExchangeRate;
                resettleDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.ResettleRNG_PG(tpCustInfo, resettleDataRNG_PG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                resettleDataRNG_PG.BetAmount /= tpCurrency.ExchangeRate;
                resettleDataRNG_PG.WinAmount /= tpCurrency.ExchangeRate;
                resettleDataRNG_PG.TransferAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        #region Sexy

        public int CancelBetLC_Sexy(BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, cancelBetTxn.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
            var result = settledService.CancelBetLC_Sexy(tpCustInfo, cancelBetTxn, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int SettledLC_Sexy(BettingDataLC_Sexy_Settle bettingDataLC_Sexy_Settle, out decimal afterAmount)
        {

            foreach (var txn in bettingDataLC_Sexy_Settle.Txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.UserId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    txn.BetAmount *= tpCurrency.ExchangeRate;
                    txn.SettleWinLoss *= tpCurrency.ExchangeRate;
                    txn.ValidBet *= tpCurrency.ExchangeRate;
                    txn.WinLoss *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.SettledLC_Sexy(bettingDataLC_Sexy_Settle, out afterAmount);

            return result;
        }

        public int UnsettledLC_Sexy(Unsettle_VoidRound bettingDataLC_Sexy_Unsettle, out decimal afterAmount)
        {

            foreach (var txn in bettingDataLC_Sexy_Unsettle.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.UnsettledLC_Sexy(bettingDataLC_Sexy_Unsettle, out afterAmount);

            return result;
        }
        public int VoidRoundLC_Sexy(Unsettle_VoidRound voidRound, out decimal afterAmount)
        {

            foreach (var txn in voidRound.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.VoidRoundLC_Sexy(voidRound, out afterAmount);

            return result;
        }

        public int VoidTxnLC_Sexy(VoidTxn voidTxn, out decimal afterAmount)
        {

            foreach (var txn in voidTxn.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.VoidTxnLC_Sexy(voidTxn, out afterAmount);

            return result;
        }
        public int PrizeLC_Sexy(IEnumerable<Sexy_Treat> sexy_Treats, out decimal afterAmount)
        {
            var sexy_Treat = sexy_Treats.FirstOrDefault();


            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, sexy_Treat.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
            sexy_Treat.tpCustInfo = tpCustInfo;

            if (tpCurrency.ExchangeRate > 0)
            {
                sexy_Treat.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PrizeLC_Sexy(sexy_Treats, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        #endregion

        #region Sexy (AWC)

        public int CancelBetLC_AWC_Sexy(CancelLC_Sexy cancelLC_Sexy, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, cancelLC_Sexy.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetLC_AWC_Sexy(tpCustInfo, cancelLC_Sexy, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelTipLC_AWC_Sexy(CancelTipLC_Sexy cancelTipLC_Sexy, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, cancelTipLC_Sexy.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);

            var result = settledService.CancelTipLC_AWC_Sexy(tpCustInfo, cancelTipLC_Sexy, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int VoidBetLC_AWC_Sexy(VoidBetLC_Sexy voidBetLC_Sexy)
        {
            foreach (var item in voidBetLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.gameInfo.realWinAmt *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.VoidBetLC_AWC_Sexy(voidBetLC_Sexy);
            return result;
        }

        public int PayoutLC_AWC_Sexy(PayoutLC_Sexy payoutLC_Sexy, out decimal afterAmount)
        {
            foreach (var txn in payoutLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    txn.turnover *= tpCurrency.ExchangeRate;
                    txn.betAmount *= tpCurrency.ExchangeRate;
                    txn.winAmount *= tpCurrency.ExchangeRate;
                    txn.gameInfo.winLoss *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutLC_AWC_Sexy(payoutLC_Sexy, out afterAmount);
            return result;
        }

        public int UnPayoutLC_AWC_Sexy(UnPayoutLC_Sexy unPayoutLC_Sexy)
        {
            foreach (var item in unPayoutLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.UnPayoutLC_AWC_Sexy(unPayoutLC_Sexy);
            return result;
        }

        public int VoidSettleLC_AWC_Sexy(VoidSettleLC_Sexy voidSettleLC_Sexy)
        {
            foreach (var item in voidSettleLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.VoidSettleLC_AWC_Sexy(voidSettleLC_Sexy);
            return result;
        }

        public int RewardLC_AWC_Sexy(RewardLC_Sexy rewardLC_Sexy, out decimal afterAmount)
        {
            foreach (var item in rewardLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.amount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.RewardLC_AWC_Sexy(rewardLC_Sexy, out afterAmount);
            return result;
        }

        public int ResettleLC_AWC_Sexy(ResettleLC_Sexy resettleLC_Sexy)
        {
            foreach (var txn in resettleLC_Sexy.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    txn.turnover *= tpCurrency.ExchangeRate;
                    txn.betAmount *= tpCurrency.ExchangeRate;
                    txn.winAmount *= tpCurrency.ExchangeRate;
                    txn.gameInfo.winLoss *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.ResettleLC_AWC_Sexy(resettleLC_Sexy);
            return result;
        }

        public IEnumerable<SettleData> GetUnsettledListLC_AWC_Sexy()
        {
            return settledService.GetUnsettledListLC_AWC_Sexy();
        }

        public int SetSettleFlag_BettransLC_AWC_Sexy(string TransId)
        {
            return settledService.SetSettleFlag_BettransLC_AWC_Sexy(TransId);
        }
        #endregion

        #region EBet

        public int PayoutLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.totalBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.validBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.payout *= tpCurrency.ExchangeRate;
                foreach (var item in bettingData_EBet.detail.betList)
                {
                    item.betMoney *= tpCurrency.ExchangeRate;
                    item.payout *= tpCurrency.ExchangeRate;
                    item.validBet *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            else if (result == -1 && tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PreRefundLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PreRefundLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            else if (result == -1 && tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.totalBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.validBet *= tpCurrency.ExchangeRate;
                foreach (var item in bettingData_EBet.detail.betList)
                {
                    item.betMoney *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.CancelBetLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            else if (result == -1 && tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RefundLC_EBet(RefundListEbet refundListEbet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, refundListEbet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                refundListEbet.refundMoney *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundLC_EBet(tpCustInfo, refundListEbet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RefundBatchLC_EBet(BatchRefundList batchRefundList, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, batchRefundList.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                batchRefundList.refundMoney *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundBatchLC_EBet(tpCustInfo, batchRefundList, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RewardLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RewardLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            else if (result == -1 && tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleData> GetUnsettledListLC_EBet()
        {
            return settledService.GetUnsettledListLC_EBet();
        }

        public SettleData GetUnsettledListLC_EBet_ByTrans(string transId)
        {
            return settledService.GetUnsettledListLC_EBet_ByTrans(transId);
        }

        public int SetSettleFlag_EBet(string TransId)
        {
            return settledService.SetSettleFlag_EBet(TransId);
        }

        public IEnumerable<CreditRecord_EBet> GetCreditRecordLC_EBet(string username, string seqNo)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EBet, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            string[] list = seqNo.Split(',');
            var data = settledService.GetCreditRecordLC_EBet(list);
            foreach (var record in data)
            {
                record.money /= tpCurrency.ExchangeRate;
            }
            return data;
        }

        #endregion

        #region CQ9

        public int PayoutRNG_CQ9(EndRoundDataRNG_CQ9 endRoundDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            afterAmount = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, endRoundDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);
            bool needInitialAfterAmount = true;

            if (tpCurrency.ExchangeRate > 0)
            {
                foreach (var item in endRoundDataRNG_CQ9.detail)
                {
                    if (item.amount > 0)
                    {
                        needInitialAfterAmount = false;
                    }

                    item.amount *= tpCurrency.ExchangeRate;
                }
            }

            if (needInitialAfterAmount)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.CQ9, endRoundDataRNG_CQ9.account, gameType);
                afterAmount = tpCustBalance.Balance;

                if (tpCurrency.ExchangeRate > 0)
                {
                    afterAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutRNG_CQ9(tpCustInfo, endRoundDataRNG_CQ9, ref afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RefundRNG_CQ9(RefundDataRNG_CQ9 refundDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, refundDataRNG_CQ9.account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                refundDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            result = gameType switch
            {
                (int)GameType.CQ9_Fish => settledService.RefundFish_CQ9(tpCustInfo, refundDataRNG_CQ9, out afterAmount),
                (int)GameType.CQ9_RNG => settledService.RefundRNG_CQ9(tpCustInfo, refundDataRNG_CQ9,
                    refundDataRNG_CQ9.amount, refundDataRNG_CQ9.roundid, out afterAmount),
                (int)GameType.CQ9_WCC => settledService.RefundWCC_CQ9(tpCustInfo, refundDataRNG_CQ9, out afterAmount),
                _ => result
            };

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int DebitRNG_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var result = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            if (gameType == (int)GameType.CQ9_Fish)
            {
                result = settledService.DebitFish_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);
            }
            else
            {
                result = settledService.DebitRNG_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);
            }

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CreditRNG_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var result = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }
            if (gameType == (int)GameType.CQ9_Fish)
            {
                result = settledService.CreditFish_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);
            }
            else
            {
                result = settledService.CreditRNG_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);
            }

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RewardRNG_CQ9(PayoffDataRNG_CQ9 payoffDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, payoffDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoffDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RewardRNG_CQ9(tpCustInfo, payoffDataRNG_CQ9, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RollinFish_CQ9(RollinDataFish_CQ9 rollinDataFish_CQ9, GameType gameType, out decimal afterAmount)
        {
            afterAmount = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, rollinDataFish_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                rollinDataFish_CQ9.validbet *= tpCurrency.ExchangeRate;
                rollinDataFish_CQ9.bet *= tpCurrency.ExchangeRate;
                rollinDataFish_CQ9.win *= tpCurrency.ExchangeRate;
                rollinDataFish_CQ9.roomfee *= tpCurrency.ExchangeRate;
                rollinDataFish_CQ9.amount *= tpCurrency.ExchangeRate;
                rollinDataFish_CQ9.rake *= tpCurrency.ExchangeRate;
            }

            if (rollinDataFish_CQ9.amount == 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.CQ9, rollinDataFish_CQ9.account, (int)gameType);
                afterAmount = tpCustBalance.Balance;
                if (tpCurrency.ExchangeRate > 0)
                {
                    afterAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.RollinFish_CQ9(tpCustInfo, rollinDataFish_CQ9, gameType, ref afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RollinWCC_CQ9(RollinDataFish_CQ9 rollinDataWCC_CQ9, GameType gameType, out decimal afterAmount)
        {
            //共用Fish rollin欄位
            afterAmount = 0;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, rollinDataWCC_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                rollinDataWCC_CQ9.validbet *= tpCurrency.ExchangeRate;
                rollinDataWCC_CQ9.bet *= tpCurrency.ExchangeRate;
                rollinDataWCC_CQ9.win *= tpCurrency.ExchangeRate;
                rollinDataWCC_CQ9.roomfee *= tpCurrency.ExchangeRate;
                rollinDataWCC_CQ9.amount *= tpCurrency.ExchangeRate;
                rollinDataWCC_CQ9.rake *= tpCurrency.ExchangeRate;
            }

            if (rollinDataWCC_CQ9.amount == 0)
            {
                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.CQ9, rollinDataWCC_CQ9.account, (int)gameType);
                afterAmount = tpCustBalance.Balance;
                if (tpCurrency.ExchangeRate > 0)
                {
                    afterAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.RollinWCC_CQ9(tpCustInfo, rollinDataWCC_CQ9, (int)gameType, ref afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public SettleDataRNG_CQ9 GetUnsettledListRNG_CQ9(string transId)
        {
            return settledService.GetUnsettledListRNG_CQ9(transId);
        }

        public int SetSettleFlagRNG_CQ9(string roundid, int SettleFlag)
        {
            return settledService.SetSettleFlagRNG_CQ9(roundid, SettleFlag);
        }

        public SettleDataRNG_CQ9 GetUnsettledListFish_CQ9(string transId)
        {
            return settledService.GetUnsettledListFish_CQ9(transId);
        }

        public int SetSettleFlagFish_CQ9(string roundid, int SettleFlag)
        {
            return settledService.SetSettleFlagFish_CQ9(roundid, SettleFlag);
        }

        public SettleDataRNG_CQ9 GetUnsettledListWCC_CQ9(string transId)
        {
            return settledService.GetUnsettledListWCC_CQ9(transId);
        }

        public int SetSettleFlagWCC_CQ9(string roundid, int SettleFlag)
        {
            return settledService.SetSettleFlagWCC_CQ9(roundid, SettleFlag);
        }

        public IEnumerable<Record_CQ9> GetRecordRNG_CQ9(string mtcode)
        {
            var data = settledService.GetRecordRNG_CQ9(mtcode);

            if (data.Count() > 0)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CQ9, data.ElementAt(0).account);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

                if (tpCurrency.ExchangeRate > 0)
                {
                    foreach (var item in data)
                    {
                        item.amount /= tpCurrency.ExchangeRate;
                        item.Before /= tpCurrency.ExchangeRate;
                        item.Balance /= tpCurrency.ExchangeRate;
                    }
                }
                data.ElementAt(0).currency = tpCurrency.TpCurrencyId;
            }
            return data;
        }


        #endregion

        #region WM

        public int PayoutLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
                bettingDataLC_WM.RealBetAmount = bettingDataLC_WM.DetailSumAmount * tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
                bettingDataLC_WM.RealBetAmount = bettingDataLC_WM.DetailSumAmount * tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int ResettleLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
                bettingDataLC_WM.RealBetAmount = bettingDataLC_WM.DetailSumAmount * tpCurrency.ExchangeRate;
            }

            var result = settledService.ResettleLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public SettleData_WM GetUnsettled_WM(string transId)
        {
            return settledService.GetUnsettled_WM(transId);
        }

        public SettleData GetSettle_TpWagerWM(string TransId)
        {
            return settledService.GetSettle_TpWagerWM(TransId);
        }

        public int SetSettleFlag_TpWagerWM(string TransId)
        {
            return settledService.SetSettleFlag_TpWagerWM(TransId);
        }

        public int SetSettleFlag_BettransLC_WM(string TransId)
        {
            return settledService.SetSettleFlag_BettransLC_WM(TransId);
        }

        #endregion

        #region Mega

        public SettleData GetSettle_TpReportMega(string TransId)
        {
            return settledService.GetSettle_TpReportMega(TransId);
        }

        public int SetSettleFlag_TpReportMega(string TransId)
        {
            return settledService.SetSettleFlag_TpReportMega(TransId);
        }

        #endregion

        #region Iconic

        public int CancelBetRNG_Iconic(BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataRNG_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_Iconic(tpCustInfo, bettingDataRNG_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutRNG_Iconic(BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataRNG_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_Iconic(tpCustInfo, bettingDataRNG_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetFish_Iconic(BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataFish_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetFish_Iconic(tpCustInfo, bettingDataFish_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutFish_Iconic(BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Iconic, bettingDataFish_Iconic.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Iconic, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_Iconic.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutFish_Iconic(tpCustInfo, bettingDataFish_Iconic, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region N2Live

        public int CancelBetLC_N2Live(CancelBetDataLC_N2Live cancelBetDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, cancelBetDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetLC_N2Live(tpCustInfo, cancelBetDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutLC_N2Live(PayoutDataLC_N2Live payoutDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, payoutDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);
            payoutDataLC_N2Live.IsVoid = false;

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_N2Live.request.totalamount *= tpCurrency.ExchangeRate;
                foreach (var item in payoutDataLC_N2Live.request.trades.trade)
                {
                    item.amount *= tpCurrency.ExchangeRate;
                    if (item.hand != null)
                    {
                        item.hand.text *= tpCurrency.ExchangeRate;
                    }
                    //牌局取消(void) - 牌局取消并获得退还
                    if (item.resultstatus != null && item.resultstatus == "void")
                    {
                        payoutDataLC_N2Live.IsVoid = true;
                    }
                }
            }

            var result = settledService.PayoutLC_N2Live(tpCustInfo, payoutDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int SmartCancelBetLC_N2Live(SmartCancelBetDataLC_N2Live smartCancelBetDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, smartCancelBetDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);

            var result = settledService.SmartCancelBetLC_N2Live(tpCustInfo, smartCancelBetDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int SmartPayoutLC_N2Live(SmartPayoutDataLC_N2Live smartPayoutDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, smartPayoutDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                smartPayoutDataLC_N2Live.request.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SmartPayoutLC_N2Live(tpCustInfo, smartPayoutDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int BonusLC_N2Live(BonusDataLC_N2Live bonusDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, bonusDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bonusDataLC_N2Live.request.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.BonusLC_N2Live(tpCustInfo, bonusDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int EventLC_N2Live(EventDataLC_N2Live eventDataLC_N2Live, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.N2Live, eventDataLC_N2Live.request.userid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.N2Live, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                eventDataLC_N2Live.request.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.EventLC_N2Live(tpCustInfo, eventDataLC_N2Live, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public SettleData_N2Live GetSettle_TpWagerLC_N2Live(string TransId)
        {
            return settledService.GetSettle_TpWagerLC_N2Live(TransId);
        }

        public SettleData GetSettle_TpSmartWagerLC_N2Live(string TransId)
        {
            return settledService.GetSettle_TpSmartWagerLC_N2Live(TransId);
        }

        public IEnumerable<SettleData> GetUnsettledList_BettransLC_N2Live()
        {
            return settledService.GetUnsettledList_BettransLC_N2Live();
        }

        public IEnumerable<SettleData_N2Live_Smart> GetUnsettledList_TpSmartWagerLC_N2Live()
        {
            return settledService.GetUnsettledList_TpSmartWagerLC_N2Live();
        }

        public int SetSettleFlag_BettransLC_N2Live(string TransId, int SettleFlag)
        {
            return settledService.SetSettleFlag_BettransLC_N2Live(TransId, SettleFlag);
        }

        public int SetSettleFlag_TpWagerLC_N2Live(string TransId)
        {
            return settledService.SetSettleFlag_TpWagerLC_N2Live(TransId);
        }

        public int SetSettleFlag_BettransSmartLC_N2Live(string TransId, int SettleFlag)
        {
            return settledService.SetSettleFlag_BettransSmartLC_N2Live(TransId, SettleFlag);
        }

        public int SetSettleFlag_TpSmartWagerLC_N2Live(string TransId)
        {
            return settledService.SetSettleFlag_TpSmartWagerLC_N2Live(TransId);
        }

        #endregion

        #region PT

        public IEnumerable<SettleData> GetUnsettledListRng_PT()
        {
            return settledService.GetUnsettledListRng_PT();
        }

        public int SetSettleFlag_TpWagerRng_PT(long TransId)
        {
            return settledService.SetSettleFlag_TpWagerRng_PT(TransId);
        }

        public IEnumerable<SettleData> GetUnsettledListLC_PT()
        {
            return settledService.GetUnsettledListLC_PT();
        }

        public int SetSettleFlag_TpWagerLC_PT(long TransId)
        {
            return settledService.SetSettleFlag_TpWagerLC_PT(TransId);
        }

        #endregion

        #region MG
        public IEnumerable<SettleData> GetUnsettledList_MG(string spName)
        {
            return settledService.GetUnsettledList_MG(spName);
        }

        public int SetSettleFlag_MG(string spName, string transId)
        {
            return settledService.SetSettleFlag_MG(spName, transId);
        }

        public int SetSettleFlag_BettransLC_MG(string spName, string transId, long transTime)
        {
            return settledService.SetSettleFlag_BettransLC_MG(spName, transId, transTime);
        }

        #endregion

        #region JILI

        public int PayoutRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, out decimal afterAmount)
        {
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.JILI, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_JILI.betAmount *= tpCurrency.ExchangeRate;
                bettingData_JILI.winloseAmount *= tpCurrency.ExchangeRate;
                bettingData_JILI.turnover *= tpCurrency.ExchangeRate;
                bettingData_JILI.preserve *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_JILI(tpCustInfo, bettingData_JILI, thirdPartyBLL, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int BetAndPayoutRNG_JILI(BettingData_JILI bettingData_JILI, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.JILI, bettingData_JILI.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.JILI, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance((int)Tp.JILI, bettingData_JILI.userId, gameType);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_JILI.betAmount *= tpCurrency.ExchangeRate;
                bettingData_JILI.winloseAmount *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_JILI(tpCustInfo, custBalance, tpCurrency, bettingData_JILI, gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return -2;
            }

            var checkBalance = custBalance.Balance - bettingData_JILI.betAmount;
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_JILI(tpCustInfo, custBalance, tpCurrency, bettingData_JILI, gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return -2;
            }

            var result = (gameType == (int)GameType.JILI_RNG) ? settledService.BetAndPayoutRNG_JILI(tpCustInfo, bettingData_JILI, thirdPartyBLL, out afterAmount) : settledService.BetAndPayoutFish_JILI(tpCustInfo, bettingData_JILI, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingData_JILI.betAmount /= tpCurrency.ExchangeRate;
                bettingData_JILI.winloseAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetRNG_JILI(CancelBetData_JILI cancelBetData_JILI, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.JILI, cancelBetData_JILI.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.JILI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                cancelBetData_JILI.betAmount *= tpCurrency.ExchangeRate;
                cancelBetData_JILI.winloseAmount *= tpCurrency.ExchangeRate;
                cancelBetData_JILI.preserve *= tpCurrency.ExchangeRate;
            }

            var result = (gameType == (int)GameType.JILI_RNG) ? settledService.CancelBetRNG_JILI(tpCustInfo, cancelBetData_JILI, out afterAmount) : settledService.CancelBetFish_JILI(tpCustInfo, cancelBetData_JILI, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                cancelBetData_JILI.betAmount /= tpCurrency.ExchangeRate;
                cancelBetData_JILI.winloseAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleData_JILI> GetUnsettledList_JILI(int category)
        {
            return settledService.GetUnsettledList_JILI(category);
        }

        public int SetSettleFlag_JILI(int category, long TransId, int TicketStatus)
        {
            return settledService.SetSettleFlag_JILI(category, TransId, TicketStatus);
        }

        public int SetSettleFlag_BettransRng_JILI(string spName, string transId, int ticketStatus, long settleTime)
        {
            return settledService.SetSettleFlag_BettransRng_JILI(spName, transId, ticketStatus, settleTime);
        }

        public IEnumerable<CheckBettingData_JILI> GetTransactionCheck_JILI(string YYYYMM)
        {
            return settledService.GetTransactionCheck_JILI(YYYYMM);
        }

        public int SetTransactionCheckSettleFlag_JILI(string YYYYMM, long TransId, int SettleFlag)
        {
            return settledService.SetTransactionCheckSettleFlag_JILI(YYYYMM, TransId, SettleFlag);
        }
        #endregion

        #region EVO

        public int CancelBetLC_EVO(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EVO, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EVO, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_EVO(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutLC_EVO(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EVO, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EVO, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_EVO(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PromoPayoutLC_EVO(PromoData_EVO promoData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.EVO, promoData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EVO, tpCustInfo.CurrencyId);

            var insertData = LoadReward_EVO(promoData_EVO, Tp.EVO, GameType.EVO_LiveCasino, tpCurrency.ExchangeRate);

            var result = settledService.PromoPayoutLC_EVO(tpCustInfo, insertData, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public Reward_EVO LoadReward_EVO(PromoData_EVO promoData_EVO, Tp tpId, GameType tpGameType, decimal rate)
        {
            if (rate > 0)
            {
                promoData_EVO.promoTransaction.amount *= rate;
                promoData_EVO.promoTransaction.playableBalance *= rate;
                if (string.Equals(promoData_EVO.promoTransaction.type, "JackpotWin", StringComparison.OrdinalIgnoreCase))
                {
                    _ = promoData_EVO.promoTransaction.jackpots.Select(x =>
                    {
                        x.winAmount *= rate;
                        return x;
                    });
                }
            }

            return new Reward_EVO()
            {
                userId = promoData_EVO.userId,
                currency = promoData_EVO.currency,
                game = (promoData_EVO.game == null) ? null : JsonConvert.SerializeObject(promoData_EVO.game),
                promoTransaction = JsonConvert.SerializeObject(promoData_EVO.promoTransaction),
                uuid = promoData_EVO.uuid,
                GameId = promoData_EVO.game?.id,
                GameType = promoData_EVO.game?.type,
                TableId = promoData_EVO.game?.details?.table?.id,
                TableVId = promoData_EVO.game?.details?.table?.vid,
                PromoTransactionType = promoData_EVO.promoTransaction.type,
                PromoTransactionId = promoData_EVO.promoTransaction.id,
                PromoTransactionAmount = Math.Abs(promoData_EVO.promoTransaction.amount),
                PromoTransactionVoucherId = promoData_EVO.promoTransaction.voucherId,
                PromoTransactionRemainingRounds = promoData_EVO.promoTransaction.remainingRounds,
                PromoTransactionPlayableBalance = promoData_EVO.promoTransaction.playableBalance,
                PromoTransactionBonusConfigId = promoData_EVO.promoTransaction.bonusConfigId,
                PromoTransactionRewardId = promoData_EVO.promoTransaction.rewardId,
                TpId = (int)tpId,
                TpGameType = (int)tpGameType,
                PaidFrom = PaidFromEnum.NVC,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related,
                SettleFlag = SettleFlag.EndGame
            };


        }

        public IEnumerable<SettleData> GetUnsettledList_EVO()
        {
            return settledService.GetUnsettledList_EVO();
        }

        public SettleData GetUnsettled_EVO(string transId)
        {
            return settledService.GetUnsettled_EVO(transId);
        }

        public int SetSettleFlag_EVO(string TransId)
        {
            return settledService.SetSettleFlag_EVO(TransId);
        }

        public SettleData_EVO GetSettle_TpWagerLC_EVO(string TransId)
        {
            return settledService.GetSettle_TpWagerLC_EVO(TransId);
        }

        public IEnumerable<Reward_EVO> GetExistReward_EVO(PromoData_EVO data, int tpId, int tpGameType)
        {
            return settledService.GetExistReward_EVO(data, tpId, tpGameType);
        }

        public IEnumerable<SettledReward_EVO> GetUnsettledRewardList_EVO()
        {
            return settledService.GetUnsettledRewardList_EVO();
        }

        public SettledReward_EVO GetUnsettledReward_EVO(string transId)
        {
            return settledService.GetUnsettledReward_EVO(transId);
        }

        public int SetRewardSettleFlag_EVO(int sq)
        {
            return settledService.SetRewardSettleFlag_EVO(sq);
        }


        #endregion

        #region Netent

        public int CancelBetRNG_Netent(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Netent, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Netent, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_Netent(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutRNG_Netent(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Netent, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Netent, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_Netent(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PromoPayoutRNG_Netent(PromoData_EVO promoData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.Netent, promoData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Netent, tpCustInfo.CurrencyId);

            var insertData = LoadReward_EVO(promoData_EVO, Tp.Netent, GameType.Netent_RNG, tpCurrency.ExchangeRate);

            var result = settledService.PromoPayoutRNG_Netent(tpCustInfo, insertData, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public SettleData GetUnsettledList_Netent(string transactionRefId)
        {
            return settledService.GetUnsettledList_Netent(transactionRefId);
        }

        public int SetSettleFlag_Netent(string TransId)
        {
            return settledService.SetSettleFlag_Netent(TransId);
        }

        #endregion

        #region RedTiger

        public int CancelBetRNG_RedTiger(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.RedTiger, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.RedTiger, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_RedTiger(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutRNG_RedTiger(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.RedTiger, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.RedTiger, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_RedTiger(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PromoPayoutRNG_RedTiger(PromoData_EVO promoData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.RedTiger, promoData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.RedTiger, tpCustInfo.CurrencyId);

            var insertData = LoadReward_EVO(promoData_EVO, Tp.RedTiger, GameType.RedTiger_RNG, tpCurrency.ExchangeRate);

            var result = settledService.PromoPayoutRNG_RedTiger(tpCustInfo, insertData, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public SettleData GetUnsettledList_RedTiger(string transactionRefId)
        {
            return settledService.GetUnsettledList_RedTiger(transactionRefId);
        }

        public int SetSettleFlag_RedTiger(string TransId)
        {
            return settledService.SetSettleFlag_RedTiger(TransId);
        }

        #endregion

        #region NLC

        public int CancelBetRNG_NLC(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.NLC, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.NLC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_NLC(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutRNG_NLC(BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.NLC, bettingData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.NLC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EVO.transaction.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_NLC(tpCustInfo, bettingData_EVO, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PromoPayoutRNG_NLC(PromoData_EVO promoData_EVO, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.NLC, promoData_EVO.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.NLC, tpCustInfo.CurrencyId);

            var insertData = LoadReward_EVO(promoData_EVO, Tp.NLC, GameType.NLC_RNG, tpCurrency.ExchangeRate);

            var result = settledService.PromoPayoutRNG_NLC(tpCustInfo, insertData, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region AP
        public int CancelBetRNG_AP(CancelBetDataRNG_AP cancelBetDataRNG_AP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, cancelBetDataRNG_AP.playerid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                cancelBetDataRNG_AP.totalstake *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_AP(tpCustInfo, cancelBetDataRNG_AP, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int SettledRNG_AP(BettingDataRNG_AP bettingDataRNG_AP, string TpUserName, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, TpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_AP.totalwin *= tpCurrency.ExchangeRate;
                bettingDataRNG_AP.totalstake *= tpCurrency.ExchangeRate;
                bettingDataRNG_AP.jpwin *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_AP(tpCustInfo, bettingDataRNG_AP, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RewardRNG_AP(CPSettleDataRNG_AP rewardDataRNG_AP, string TpUserName, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, TpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                rewardDataRNG_AP.cpwin *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RewardRNG_AP(tpCustInfo, rewardDataRNG_AP, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int Payout_AdvantPlayMini(PayoutData_AdvantPlayMini payoutData_AdvantPlayMini, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, payoutData_AdvantPlayMini.UserReferenceId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                payoutData_AdvantPlayMini.StakeAmount *= tpCurrency.ExchangeRate;
                payoutData_AdvantPlayMini.WonAmount *= tpCurrency.ExchangeRate;

                if (payoutData_AdvantPlayMini.WinResults.Any())
                {
                    foreach (var item in payoutData_AdvantPlayMini.WinResults)
                    {
                        item.BuyAmount *= tpCurrency.ExchangeRate;
                        item.WonAmount *= tpCurrency.ExchangeRate;
                    }
                }
            }

            var result = settledService.Payout_AdvantPlayMini(tpCustInfo, payoutData_AdvantPlayMini, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int Result_AdvantPlayMini(ResultData_AdvantPlayMini resultData_AdvantPlayMini)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, resultData_AdvantPlayMini.UserReferenceId);
            var result = settledService.Result_AdvantPlayMini(tpCustInfo, resultData_AdvantPlayMini);
            return result;
        }

        public int CancelBet_AdvantPlayMini(CancelBetData_AdvantPlayMini cancelBetData_AdvantPlayMini)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, cancelBetData_AdvantPlayMini.UserReferenceId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var item in cancelBetData_AdvantPlayMini.Data)
                {
                    item.Amount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.CancelBet_AdvantPlayMini(tpCustInfo, cancelBetData_AdvantPlayMini);
            return result;
        }

        public int CancelPayout_AdvantPlayMini(CancelBetData_AdvantPlayMini cancelBetData_AdvantPlayMini)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AP, cancelBetData_AdvantPlayMini.UserReferenceId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var item in cancelBetData_AdvantPlayMini.Data)
                {
                    item.Amount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.CancelPayout_AdvantPlayMini(tpCustInfo, cancelBetData_AdvantPlayMini);
            return result;
        }

        public int SetSettleFlag_BettransRng_AP_Mini(string spName, string transId, long settleTime)
        {
            return settledService.SetSettleFlag_BettransRng_AP_Mini(spName, transId, settleTime);
        }


        #endregion

        #region YGG

        public SettleData_YGG GetSettle_TpWager_YGG(string transId)
        {
            return settledService.GetSettle_TpWager_YGG(transId);
        }

        #endregion

        #region Kiss918

        public IEnumerable<SettleData_Kiss918> GetSettle_TpWager_Kiss918(string playSessionID, string referenceID)
        {
            return settledService.GetSettle_TpWager_Kiss918(playSessionID, referenceID);
        }
        #endregion

        #region SimplePlay

        public int PayoutRNG_SimplePlay(PayoutData_SimplePlay payoutData_SimplePlay, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.SimplePlay;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, payoutData_SimplePlay.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutData_SimplePlay.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_SimplePlay(tpCustInfo, payoutData_SimplePlay, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetRNG_SimplePlay(CancelBetData_SimplePlay cancelBetData_SimplePlay, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.SimplePlay;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, cancelBetData_SimplePlay.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                cancelBetData_SimplePlay.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_SimplePlay(tpCustInfo, cancelBetData_SimplePlay, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region Skywind
        public int Settled_Skywind(TpCustInfo tpCustInfo, BettingData_Skywind bettingData_Skywind, string TpUserName, int gameType, out decimal afterAmount)
        {
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_Skywind.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.Settled_Skywind(tpCustInfo, bettingData_Skywind, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            if (afterAmount == 0)
            {
                afterAmount = tpCustInfoBLL.GetBalance((int)Tp.Skywind, TpUserName, gameType).Balance;
            }

            return result;
        }

        public int CancelBet_Skywind(TpCustInfo tpCustInfo, CancelBetData_Skywind cancelBetData_Skywind, int gameType, out decimal afterAmount)
        {
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Skywind, tpCustInfo.CurrencyId);


            var result = settledService.CancelBet_Skywind(tpCustInfo, cancelBetData_Skywind, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public SettleData_Skywind GetSettle_TpWager_Skywind(string transId)
        {
            return settledService.GetSettle_TpWager_Skywind(transId);
        }

        #endregion

        #region Yeebet

        public IEnumerable<SettleData_Yeebet> GetSettle_TpWagerLC_Yeebet(string serialnumber)
        {
            return settledService.GetSettle_TpWagerLC_Yeebet(serialnumber);
        }

        #endregion

        #region BG

        public IEnumerable<SettleData_BG> GetSettle_TpWagerLC_BG(string orderId)
        {
            return settledService.GetSettle_TpWagerLC_BG(orderId);
        }

        #endregion

        #region SBSW

        public IEnumerable<SBSWUnsettle> GetSBSWUnsettleList(int LimitCount = 5000)
        {
            return settledService.GetSBSWUnsettleList(LimitCount);
        }

        public BetRecordInfo GetSBSWTpBetRecord(int TpId, string TransId, int GameType)
        {
            return settledService.GetSBSWUnsettleList(TpId, TransId, GameType);
        }

        #endregion

        #region PlayTech

        public int Payout_PlayTech(BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            var tpId = (int)Tp.PlayTech;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_PlayTech.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var data in bettingData_PlayTech.trans)
                {
                    data.amount *= tpCurrency.ExchangeRate;
                    data.jpc *= tpCurrency.ExchangeRate;
                    data.jpw *= tpCurrency.ExchangeRate;
                    data.validBet *= tpCurrency.ExchangeRate;
                    data.validWin *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.Payout_PlayTech(tpCustInfo, bettingData_PlayTech, gameType, partitionCheckDay, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBet_PlayTech(BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            var tpId = (int)Tp.PlayTech;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_PlayTech.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var data in bettingData_PlayTech.trans)
                {
                    data.amount *= tpCurrency.ExchangeRate;
                    data.jpc *= tpCurrency.ExchangeRate;
                    data.jpw *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.CancelBet_PlayTech(tpCustInfo, bettingData_PlayTech, gameType, partitionCheckDay, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int BetAndPayout_PlayTech(BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            var tpId = (int)Tp.PlayTech;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_PlayTech.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var custBalance = tpCustInfoBLL.GetBalance(tpId, bettingData_PlayTech.playerId, (int)gameType);

            decimal totalBet = 0;
            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var data in bettingData_PlayTech.trans)
                {
                    data.amount *= tpCurrency.ExchangeRate;
                    data.jpc *= tpCurrency.ExchangeRate;
                    data.jpw *= tpCurrency.ExchangeRate;
                    if (string.Equals(data.transType, "bet", StringComparison.OrdinalIgnoreCase))
                    {
                        totalBet += data.amount;
                    }
                }
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_PlayTech(tpCustInfo, custBalance, tpCurrency, bettingData_PlayTech, (int)gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var checkBalance = custBalance.Balance - totalBet;
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = settledService.GetTpBetFailureParam_PlayTech(tpCustInfo, custBalance, tpCurrency, bettingData_PlayTech, (int)gameType, TpCheckRecordCode.InsufficientBalance, afterAmount);
                thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var result = settledService.BetAndPayout_PlayTech(tpCustInfo, bettingData_PlayTech, gameType, partitionCheckDay, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int Reward_PlayTech(RewardData_PlayTech rewardData_PlayTech, out decimal afterAmount)
        {
            var tpId = (int)Tp.PlayTech;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, rewardData_PlayTech.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                rewardData_PlayTech.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.Reward_PlayTech(tpCustInfo, rewardData_PlayTech, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        #region Endorphina

        public int CancelBetRNG_Endorphina(CancelBetData_Endorphina cancelBetData_Endorphina, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.Endorphina;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, cancelBetData_Endorphina.player);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                cancelBetData_Endorphina.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_Endorphina(tpCustInfo, cancelBetData_Endorphina, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutRNG_Endorphina(PayoutData_Endorphina payoutData_Endorphina, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.Endorphina;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, payoutData_Endorphina.player);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                payoutData_Endorphina.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_Endorphina(tpCustInfo, payoutData_Endorphina, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int EndSessionRNG_Endorphina(EndSessionData_Endorphina endSessionData_Endorphina, int gameType, out decimal afterAmount)
        {
            var tpId = (int)Tp.Endorphina;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(tpId, endSessionData_Endorphina.player);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                endSessionData_Endorphina.bet *= tpCurrency.ExchangeRate;
                endSessionData_Endorphina.bonusBet *= tpCurrency.ExchangeRate;
                endSessionData_Endorphina.bonusWin *= tpCurrency.ExchangeRate;
                endSessionData_Endorphina.jackpotWin *= tpCurrency.ExchangeRate;
                endSessionData_Endorphina.win *= tpCurrency.ExchangeRate;
            }

            return settledService.EndSessionRNG_Endorphina(tpCustInfo, endSessionData_Endorphina, gameType, out afterAmount);
        }

        #endregion

        #region DragonGaming

        public int SetSettleFlag_DragonGaming(string transId)
        {
            return settledService.SetSettleFlag_DragonGaming(transId);
        }

        #endregion
    }
}