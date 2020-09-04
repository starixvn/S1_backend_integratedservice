using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class BettingBLL
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

        public int PlaceBetLC_AG(BettingDataLC_AG bettingDataLC_AG, out decimal afterAmount)
        {
            string username = bettingDataLC_AG.Playname.Substring(bettingDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_AG.Value *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_AG(tpCustInfo, bettingDataLC_AG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate; /*bettingDataLC_SA.Betdetails has not fixed*/
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

        public int PlaceBetLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.CancelBetLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_GPI(BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.GPI, bettingDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_GPI(tpCustInfo, bettingDataRNG_GPI, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PlaceBetRNG_Joker(BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Joker, bettingDataRNG_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_Joker.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_Joker(tpCustInfo, bettingDataRNG_Joker, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_Joker.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int PlaceBetRNG_YGG(BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.YGG, bettingDataRNG_YGG.playerid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_YGG.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_YGG(tpCustInfo, bettingDataRNG_YGG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_YGG.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int PlaceBetRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_SG(tpCustInfo, bettingDataRNG_SG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PlaceBetRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_PP(tpCustInfo, bettingDataRNG_PP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PlaceBetLC_Allbet(BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Allbet, bettingDataLC_Allbet.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Allbet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_Allbet.Amount *= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.PlaceBetLC_Allbet(tpCustInfo, bettingDataLC_Allbet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount /= tpCurrency.ExchangeRate;
                }
            }
            return result;
        }
        public int CancelBetLC_Allbet(BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Allbet, bettingDataLC_Allbet.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Allbet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_Allbet.Amount *= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.CancelBetLC_Allbet(tpCustInfo, bettingDataLC_Allbet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount /= tpCurrency.ExchangeRate;
                }
            }
            return result;
        }

        public int PlaceBetRNG_PG(BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PG, bettingDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetRNG_PG(tpCustInfo, bettingDataRNG_PG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PlaceBetLC_Sexy(BettingDataLC_Sexy_Bet bettingDataLC_Sexy_Bet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, bettingDataLC_Sexy_Bet.UserId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                foreach (var txn in bettingDataLC_Sexy_Bet.Txns)
                {
                    txn.BetAmount *= tpCurrency.ExchangeRate;
                }
            }

            var result = bettingService.PlaceBetLC_Sexy(tpCustInfo, bettingDataLC_Sexy_Bet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetLC_Sexy(BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, cancelBetTxn.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
            var result = bettingService.CancelBetLC_Sexy(tpCustInfo, cancelBetTxn, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
    }
}
