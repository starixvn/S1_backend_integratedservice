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
    }
}
