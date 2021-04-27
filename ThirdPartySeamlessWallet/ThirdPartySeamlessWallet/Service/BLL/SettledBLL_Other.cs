using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public IEnumerable<SettleLT_One789> GetUnsettledList_One789(int gameType)
        {
            return settledService.GetUnsettledList_One789(gameType);
        }

        public SettleLT_One789 GetSettle_TpWagerOne789(int gameType, string TransId)
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

        #region OneGaming

        public int MoneyInPK_OneGaming(BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, bettingDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.MoneyInPK_OneGaming(tpCustInfo, bettingDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RollbackPK_OneGaming(RollbackDataPK_OneGaming rollbackDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, rollbackDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                rollbackDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RollbackPK_OneGaming(tpCustInfo, rollbackDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleDataPK_OneGaming> GetUnsettledListPK_OneGaming()
        {
            return settledService.GetUnsettledListPK_OneGaming();
        }

        public int SetSettleFlag_TpWagerPK_OneGaming(string ticket)
        {
            return settledService.SetSettleFlag_TpWagerPK_OneGaming(ticket);
        }

        public int Settled_OneGaming(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            return settledService.Settled_OneGaming(tpCustInfo, betRecordInfo);
        }

        #endregion

        #region V8

        public IEnumerable<SettleDataPK_V8> GetUnsettledListPK_V8()
        {
            return settledService.GetUnsettledListPK_V8();
        }

        public int SetSettleFlag_TpWagerPK_V8(string gameId)
        {
            return settledService.SetSettleFlag_TpWagerPK_V8(gameId);
        }

        #endregion

        #region BTI
                    
        public IEnumerable<SettleData> GetUnsettledListBTI()
        {
            return settledService.GetUnsettledList_BTI();
        }

        public int SetSettleFlag_BTI(string Account, string TransId)
        {
            return settledService.SetSettleFlag_BTI(Account, TransId);
        }

        #endregion
    }
}
