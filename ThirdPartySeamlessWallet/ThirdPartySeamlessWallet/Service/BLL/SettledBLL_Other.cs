using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class SettledBLL
    {
        public int Settled_Without_Bonus(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            return settledService.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
        }

        #region CF

        public IEnumerable<PayoutData_CF> GetUnPayoutList_CF()
        {
            return settledService.GetUnPayoutList_CF();
        }

        public IEnumerable<SettleData_CF> GetUnSettleList_CF()
        {
            return settledService.GetUnSettleList_CF();
        }

        public int Payout_CF(PayoutData_CF payoutData_CF, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CF, payoutData_CF.login_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CF, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutData_CF.payout *= tpCurrency.ExchangeRate;
                payoutData_CF.winloss *= tpCurrency.ExchangeRate;
            }

            var result = settledService.Payout_CF(tpCustInfo, payoutData_CF, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int Settled_CF(SettleData_CF settleData_CF, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CF, settleData_CF.login_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CF, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settleData_CF.stake *= tpCurrency.ExchangeRate;
                settleData_CF.stake_money *= tpCurrency.ExchangeRate;
                settleData_CF.payout *= tpCurrency.ExchangeRate;
                settleData_CF.winloss *= tpCurrency.ExchangeRate;
            }

            var result = settledService.Settled_CF(tpCustInfo, settleData_CF, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        #region AWC

        public int PayoutCF_AWC(PayoutCF_AWC payoutCF_AWC, out decimal afterAmount)
        {

            foreach (var item in payoutCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.winAmount *= tpCurrency.ExchangeRate;
                    item.turnover *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.PayoutCF_AWC(payoutCF_AWC, out afterAmount);
            return result;
        }

        public int UnPayoutCF_AWC(UnPayoutCF_AWC unPayoutCF_AWC, out decimal afterAmount)
        {
            foreach (var item in unPayoutCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.UnPayoutCF_AWC(unPayoutCF_AWC, out afterAmount);
            return result;
        }

        public int VoidSettleCF_AWC(VoidSettleCF_AWC voidSettleCF_AWC, out decimal afterAmount)
        {
            foreach (var item in voidSettleCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.VoidSettleCF_AWC(voidSettleCF_AWC, out afterAmount);
            return result;
        }

        public int UnvoidSettleCF_AWC(UnVoidSettleCF_AWC unVoidSettleCF_AWC, out decimal afterAmount)
        {
            foreach (var item in unVoidSettleCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;
            }
            var result = settledService.UnvoidSettleCF_AWC(unVoidSettleCF_AWC, out afterAmount);
            return result;
        }

        public int RefundCF_AWC(RefundCF_AWC refundCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, refundCF_AWC.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            foreach (var item in refundCF_AWC.txns)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.winAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.RefundCF_AWC(tpCustInfo, refundCF_AWC, out afterAmount);
            return result;
        }

        public int RewardCF_AWC(RewardCF_AWC rewardCF_AWC, out decimal afterAmount)
        {
            foreach (var item in rewardCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.amount *= tpCurrency.ExchangeRate;
                }
            }
            var result = settledService.RewardCF_AWC(rewardCF_AWC, out afterAmount);
            return result;
        }

        public IEnumerable<SettleCF_AWC> GetUnsettledListCF_AWC()
        {
            return settledService.GetUnsettledListCF_AWC();
        }

        public int Settled_AWC(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            return settledService.Settled_AWC(tpCustInfo, betRecordInfo);
        }

        #endregion

        #region One789

        public int PayoutLT_One789(BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.One789, bettingDataLT_One789.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.One789, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLT_One789(tpCustInfo, bettingDataLT_One789, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount /= tpCurrency.ExchangeRate;
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RefundLT_One789(BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.One789, bettingDataLT_One789.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.One789, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundLT_One789(tpCustInfo, bettingDataLT_One789, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount /= tpCurrency.ExchangeRate;
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int ResettleLT_One789(BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.One789, bettingDataLT_One789.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.One789, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.ResettleLT_One789(tpCustInfo, bettingDataLT_One789, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount /= tpCurrency.ExchangeRate;
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public BettingDataLT_One789 GetRecordLT_One789(string source)
        {
            return settledService.GetRecordLT_One789(source);
        }

        public SettleData GetUnsettledList_One789(int gameType, string TransId)
        {
            return settledService.GetUnsettledList_One789(gameType, TransId);
        }

        public SettleData_One789 GetSettle_TpWagerOne789(int gameType, string TransId)
        {
            return settledService.GetSettle_TpWagerOne789(gameType, TransId);
        }

        public int SetSettleFlag_TpWagerOne789(int gameType, string TransId)
        {
            return settledService.SetSettleFlag_TpWagerOne789(gameType, TransId);
        }

        public int SetSettleFlag_Bettrans_One789(int gameType, string TransId)
        {
            return settledService.SetSettleFlag_Bettrans_One789(gameType, TransId);
        }

        #endregion

        #region BTI
        
        public IEnumerable<BTISettleData> GetUnsettledListBTI()
        {
            return settledService.GetUnsettledList_BTI();
        }

        public int SetSettleFlag_BTI(string account, string transId, bool isSuccess = true)
        {
            return settledService.SetSettleFlag_BTI(account, transId, isSuccess);
        }
        
        #endregion 

        #region KM

        public IEnumerable<SettleData> GetUnsettledListPK_KM(string spName)
        {
            return settledService.GetUnsettledList_KM(spName);
        }

        public int SetSettleFlag_KM(string spName, string transId)
        {
            return settledService.SetSettleFlag_KM(spName, transId);
        }

        #endregion

        #region GPI

        public int PayoutLT_GPI(SettledDataRNG_GPI settledDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, settledDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settledDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLT_GPI(tpCustInfo, settledDataRNG_GPI, gameType, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int VoidBetLT_GPI(BettingDataRNG_GPI bettingDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, bettingDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.VoidBetLT_GPI(tpCustInfo, bettingDataRNG_GPI, gameType, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int EditBetLT_GPI(BettingDataRNG_GPI bettingDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, bettingDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.EditBetLT_GPI(tpCustInfo, bettingDataRNG_GPI, gameType, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetLT_GPI(CancelBet_GPI cancelBet_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.GPI, cancelBet_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetLT_GPI(tpCustInfo, cancelBet_GPI, gameType, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion
    }
}
