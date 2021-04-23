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
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CF, bettingData_CF.login_id);
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
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.CF, betrecord.login_id);
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

        public int PlaceBetCF_AWC(BetCF_AWC betCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, betCF_AWC.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
            foreach (var item in betCF_AWC.txns)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.gameInfo.realBetAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.PlaceBetCF_AWC(tpCustInfo, betCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetCF_AWC(CancelCF_AWC cancelCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, cancelCF_AWC.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            var result = bettingService.CancelBetCF_AWC(tpCustInfo, cancelCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int AdjustBetCF_AWC(AdjustBetCF_AWC adjustBetCF_AWC, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, adjustBetCF_AWC.txns.ElementAt(0).userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);

            foreach (var item in adjustBetCF_AWC.txns)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                    item.adjustAmount *= tpCurrency.ExchangeRate;
                    item.gameInfo.realBetAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.AdjustBetCF_AWC(tpCustInfo, adjustBetCF_AWC, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int VoidBetCF_AWC(VoidBetCF_AWC voidBetCF_AWC)
        {
            foreach (var item in voidBetCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    item.betAmount *= tpCurrency.ExchangeRate;
                }
            }
            var result = bettingService.VoidBetCF_AWC(voidBetCF_AWC);
            return result;
        }

        public int UnvoidBetCF_AWC(UnVoidBetCF_AWC unVoidBetCF_AWC)
        {
            foreach (var item in unVoidBetCF_AWC.txns)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AWC, item.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AWC, tpCustInfo.CurrencyId);
                item.tpCustInfo = tpCustInfo;
            }
            var result = bettingService.UnvoidBetCF_AWC(unVoidBetCF_AWC);
            return result;
        }

        #endregion

        #region One789

        public int PlaceBetLT_One789(BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.One789, bettingDataLT_One789.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.One789, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLT_One789(tpCustInfo, bettingDataLT_One789, gameType, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                bettingDataLT_One789.Amount /= tpCurrency.ExchangeRate;
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region OneGaming

        public int MoneyOutPK_OneGaming(BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, bettingDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.MoneyOutPK_OneGaming(tpCustInfo, bettingDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region BTI

        public int Reserve_BTI(BettingDataBTIReserve request, out decimal afterAmount, out long trx_id)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(Tp.BTI, request.cust_id);

            if (tpCurrency.ExchangeRate > 0)
            {
                request.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.Reserve_BTI(tpCustInfo, request, out afterAmount, out trx_id);
            
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion


    }
}
