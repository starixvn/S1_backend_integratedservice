using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class BettingBLL
    {
        private readonly BettingService bettingService;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public BettingBLL(BettingService bettingService, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, WalletService walletService)
        {
            this.bettingService = bettingService;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
        }
        public int PlaceBet(int tpId, string TransId, string tpUserName, decimal tpStake, GameType gameType, string betDetial, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            if (tpCurrency.ExchangeRate > 0)
            {
                tpStake *= tpCurrency.ExchangeRate;
            }
            betRecordInfo.TpId = tpId;
            betRecordInfo.Stake = tpStake;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;

            var result = bettingService.PlaceBet(tpCustInfo, betRecordInfo, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_SA(tpCustInfo, bettingDataLC_SA, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.CancelBetLC_SA(tpCustInfo, bettingDataLC_SA, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
    }
}
