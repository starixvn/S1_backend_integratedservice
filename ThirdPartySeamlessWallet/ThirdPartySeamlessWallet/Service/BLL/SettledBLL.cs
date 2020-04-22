using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class SettledBLL
    {
        private readonly SettledService settledService;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;

        public SettledBLL(SettledService settledService, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.settledService = settledService;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.thirdPartyBLL = thirdPartyBLL;
        }

        public int Refund(int tpId, string TransId, string tpUserName, decimal backAmount, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            if (tpCurrency.ExchangeRate > 0)
            {
                backAmount *= tpCurrency.ExchangeRate;
            }
            betRecordInfo.Stake = backAmount;
            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.TicketStatus = TicketStatus.Refund;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, backAmount, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int Win(int tpId, string TransId, string tpUserName, decimal wonAmount, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            if (tpCurrency.ExchangeRate > 0)
            {
                wonAmount *= tpCurrency.ExchangeRate;
            }

            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.TicketStatus = TicketStatus.Win;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, wonAmount, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int Lost(int tpId, string TransId, string tpUserName, long SettledTime, GameType gameType, out decimal tpAfterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            //tpCustBalance.ExchangeRate
            BetRecordInfo betRecordInfo = new BetRecordInfo();
            betRecordInfo.TpId = tpId;
            betRecordInfo.TransId = TransId;
            betRecordInfo.GameType = gameType;
            betRecordInfo.TpUserName = tpUserName;
            betRecordInfo.SettledTime = SettledTime;
            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
            betRecordInfo.TicketStatus = TicketStatus.Lost;

            var result = settledService.SettleTicket(tpCustInfo, betRecordInfo, 0, out tpAfterAmount);
            if (result > 0 && tpAfterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                tpAfterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int SettledLC_AG(PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_AG.NetAmount *= tpCurrency.ExchangeRate;
                payoutDataLC_AG.ValidBetAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledLC_AG(tpCustInfo, payoutDataLC_AG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int SettledLC_AG_Merger(PayoutDataLC_AG payoutDataLC_AG, string relTransId)
        {
            int result = settledService.SettledLC_AG_Merger(payoutDataLC_AG, relTransId);
            return result;
        }
        public int SettledRNG_GPI(SettledDataRNG_GPI settledDataRNG_GPI, out decimal afterAmount, out string TransId)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.GPI, settledDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settledDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_GPI(tpCustInfo, settledDataRNG_GPI, out afterAmount, out TransId);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int SettledFish_YL(BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.YL, bettingDataFish_YL.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YL, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataFish_YL.betAmount *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.payAmount *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.profit *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.validBet *= tpCurrency.ExchangeRate;
                bettingDataFish_YL.commission *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledFish_YL(tpCustInfo, bettingDataFish_YL, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                //settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }
            else
            {
                result = -1;
            }

            return result;
        }

        public int RefundFish_YL(RefundDataFish_YL refundDataFish_YL, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.YL, refundDataFish_YL.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YL, tpCustInfo.CurrencyId);

            var result = settledService.RefundFish_YL(tpCustInfo, refundDataFish_YL, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                //settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }
            else
            {
                result = -1;
            }

            return result;
        }
        public int SettledRNG_Joker(BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Joker, bettingDataRNG_Joker.username);
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
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Joker, cancelBetDataRNG_Joker.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Joker, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetRNG_Joker(tpCustInfo, cancelBetDataRNG_Joker, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int SettledRNG_YGG(SettledDataRNG_YGG settledDataRNG_YGG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.YGG, settledDataRNG_YGG.playerid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settledDataRNG_YGG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_YGG(tpCustInfo, settledDataRNG_YGG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                settledDataRNG_YGG.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int CancelBetRNG_YGG(BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.YGG, bettingDataRNG_YGG.playerid);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.YGG, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetRNG_YGG(tpCustInfo, bettingDataRNG_YGG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int SettledRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_SG(tpCustInfo, bettingDataRNG_SG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_SG.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int CancelBetRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetRNG_SG(tpCustInfo, bettingDataRNG_SG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            var result = settledService.CancelBetRNG_PP(tpCustInfo, bettingDataRNG_PP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int SettledRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            var result = settledService.SettledRNG_PP(tpCustInfo, bettingDataRNG_PP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
    }
}
