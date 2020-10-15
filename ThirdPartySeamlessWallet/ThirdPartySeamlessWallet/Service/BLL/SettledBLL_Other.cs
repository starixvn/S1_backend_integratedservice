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
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CF, payoutData_CF.login_id);
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
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CF, settleData_CF.login_id);
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

        public int PayoutCF_AWC(SettleDataCF_AWC settleDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, settleDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settleDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
                settleDataCF_AWC.winAmount *= tpCurrency.ExchangeRate;
                settleDataCF_AWC.turnover *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutCF_AWC(tpCustInfo, settleDataCF_AWC, out afterAmount);
            return result;
        }

        public int UnPayoutCF_AWC(UnsettleDataCF_AWC unsettleDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, unsettleDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                unsettleDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.UnPayoutCF_AWC(tpCustInfo, unsettleDataCF_AWC, out afterAmount);
            return result;
        }

        public int VoidSettleCF_AWC(VoidSettleDataCF_AWC voidSettleDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, voidSettleDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                voidSettleDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.VoidSettleCF_AWC(tpCustInfo, voidSettleDataCF_AWC, out afterAmount);
            return result;
        }

        public int UnvoidSettleCF_AWC(UnvoidSettleDataCF_AWC unvoidSettleDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, unvoidSettleDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
            var result = settledService.UnvoidSettleCF_AWC(tpCustInfo, unvoidSettleDataCF_AWC, out afterAmount);
            return result;
        }

        public int RefundCF_AWC(RefundCF_AWC refundCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, refundCF_AWC.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            foreach (var item in refundCF_AWC.txns)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.winAmount *= tpCurrency.ExchangeRate;
                    item.turnover *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.RefundCF_AWC(tpCustInfo, refundCF_AWC, out afterAmount);
            return result;
        }

        #endregion
    }
}
