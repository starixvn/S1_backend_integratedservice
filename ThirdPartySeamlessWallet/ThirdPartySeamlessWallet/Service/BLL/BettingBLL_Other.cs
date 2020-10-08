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

        #region CF

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

        #endregion

        #region AWC

        public int PlaceBetCF_AWC(BettingDataCF_AWC bettingDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, bettingDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetCF_AWC(tpCustInfo, bettingDataCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetCF_AWC(CancelBetDataCF_AWC cancelBetDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, cancelBetDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            var result = bettingService.CancelBetCF_AWC(tpCustInfo, cancelBetDataCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int AdjustBetCF_AWC(AdjustBetDataCF_AWC adjustBetDataCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, adjustBetDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                adjustBetDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
                adjustBetDataCF_AWC.adjustAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.AdjustBetCF_AWC(tpCustInfo, adjustBetDataCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int VoidBetCF_AWC(VoidBetDataCF_AWC voidBetDataCF_AWC)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, voidBetDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                voidBetDataCF_AWC.betAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.VoidBetCF_AWC(tpCustInfo, voidBetDataCF_AWC);

            return result;
        }

        public int UnvoidBetCF_AWC(UnvoidBetDataCF_AWC unvoidBetDataCF_AWC)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AWC, unvoidBetDataCF_AWC.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            var result = bettingService.UnvoidBetCF_AWC(tpCustInfo, unvoidBetDataCF_AWC);
            return result;
        }

        #endregion
    }
}
