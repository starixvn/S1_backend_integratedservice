using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class BettingBLL
    {
        public int PlaceBet_CF(BettingData_CF bettingData_CF, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CF, bettingData_CF.login_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CF, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_CF.stake *= tpCurrency.ExchangeRate;
                bettingData_CF.stake_money *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBet_CF(tpCustInfo, bettingData_CF, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBet_CF(CancelBetData_CF cancelBetData_CF)
        {
            var betrecord = bettingService.GetBetRecord_CF(cancelBetData_CF);
            int result = 0;
            if (betrecord != null)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CF, betrecord.login_id);
                result = bettingService.CancelBet_CF(tpCustInfo, cancelBetData_CF);
            }
            else
            {
                return 1;
            }

            return result;
        }
    }
}
