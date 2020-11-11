using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class SettledBLL
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
        public int Settled(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, TpCategory tpCategory)
        {
            var result = settledService.Settle(tpCustInfo, betRecordInfo, tpCategory);
            return result;
        }
        public SettlementType GetSettlementType(GameType gameType, string gameId)
        {
            return settledService.GetSettlementType(gameType, gameId);
        }
        public IEnumerable<PayoutDataLC_SA> GetUnsettledListLC_SA()
        {
            return settledService.GetUnsettledListLC_SA();
        }
        public int CancelBetLC_SA(BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SA, bettingDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_SA(tpCustInfo, bettingDataLC_SA, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PayoutLC_SA(PayoutDataLC_SA payoutDataLC_SA, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SA, payoutDataLC_SA.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SA, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_SA.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_SA(tpCustInfo, payoutDataLC_SA, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int Get_TpWagerSA_Settle(string GameID, string Username, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            return settledService.Get_TpWagerSA_Settle(GameID, Username, out stake, out validBet, out winLostAmount, out settleTime);
        }
        public int SetSettleFlag_TpWagerSA(string GameID, string Username)
        {
            return settledService.SetSettleFlag_TpWagerSA(GameID, Username);
        }
        public int SetSettleFlag_SA(string GameID, string Username)
        {
            return settledService.SetSettleFlag_SA(GameID, Username);
        }
        public int CancelBetLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int PayoutLC_DG(BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.DG, bettingDataLC_DG.Username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.DG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_DG.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_DG(tpCustInfo, bettingDataLC_DG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<BettingDataLC_DG> GetUnsettledListLC_DG()
        {
            return settledService.GetUnsettledListLC_DG();
        }
        public int Get_TpWagerDG_Settle(string TransId, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            return settledService.Get_TpWagerDG_Settle(TransId, out stake, out validBet, out winLostAmount, out settleTime);
        }
        public int SetSettleFlag_DG(string TransId)
        {
            return settledService.SetSettleFlag_DG(TransId);
        }
        public int SetSettleFlag_TpWagerDG(string transId)
        {
            return settledService.SetSettleFlag_TpWagerDG(transId);
        }
        public int PayoutLC_AG(PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataLC_AG.NetAmount *= tpCurrency.ExchangeRate;
                payoutDataLC_AG.ValidBetAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_AG(tpCustInfo, payoutDataLC_AG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<DataLC_AG> GetUnsettledListLC_AG()
        {
            return settledService.GetUnsettledListLC_AG();
        }
        public int RefundLC_AG(BettingDataLC_AG_Refund bettingDataLC_AG_Refund, out decimal afterAmount)
        {
            string username = bettingDataLC_AG_Refund.Playname.Substring(bettingDataLC_AG_Refund.AgentCode.Length);
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_AG_Refund.Value *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundLC_AG(tpCustInfo, bettingDataLC_AG_Refund, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<SettleDataLC_AG> Get_PayoutList_AG(string transactionID)
        {
            return settledService.Get_PayoutList_AG(transactionID);
        }
        public IEnumerable<BettingDataLC_AG> Get_BetList_AG(string transactionIDs)
        {
            return settledService.Get_BetList_AG(transactionIDs);
        }
        public int SetSettleFlag_AG(string transactionID, string transactionIDs)
        {
            return settledService.SetSettleFlag_AG(transactionID, transactionIDs);
        }
        public int SetSettleFlag_TpWagerAG(string billNoList)
        {
            return settledService.SetSettleFlag_TpWagerAG(billNoList);
        }
        public int Get_TpWagerAG_Settle(string billNoList, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            var result = settledService.Get_TpWagerAG_Settle(billNoList, out stake, out validBet, out winLostAmount, out settleTime);
            return result;
        }
        public int PayoutRNG_GPI(SettledDataRNG_GPI settledDataRNG_GPI, out decimal afterAmount, out string TransId)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.GPI, settledDataRNG_GPI.cust_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.GPI, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                settledDataRNG_GPI.amount *= tpCurrency.ExchangeRate;
                if ((settledDataRNG_GPI.jackpot_win ?? 0m) != 0m)
                {
                    settledDataRNG_GPI.jackpot_win *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutRNG_GPI(tpCustInfo, settledDataRNG_GPI, out afterAmount, out TransId);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                settledDataRNG_GPI.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<SettleDataRNG_GPI> GetUnsettledListRNG_GPI()
        {
            return settledService.GetUnsettledListRNG_GPI();
        }
        public int SetSettleFlagRNG_GPI(string roundId, string custId, int SettleFlag)
        {
            return settledService.SetSettleFlagRNG_GPI(roundId, custId, SettleFlag);
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
                settledDataRNG_YGG.bonusprize *= tpCurrency.ExchangeRate;
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
        public int PayoutRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
                bettingDataRNG_PP.promoWinAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_PP(tpCustInfo, bettingDataRNG_PP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int BonusWinRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
                bettingDataRNG_PP.promoWinAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.BonusWinRNG_PP(tpCustInfo, bettingDataRNG_PP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #region SG

        public int CancelBetRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetRNG_SG(tpCustInfo, bettingDataRNG_SG, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int PayoutRNG_SG(BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, bettingDataRNG_SG.acctId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_SG.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutRNG_SG(tpCustInfo, bettingDataRNG_SG, gameType, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_SG.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public IEnumerable<SettleData_SG> GetUnsettledList_SG(int gameType)
        {
            return settledService.GetUnsettledList_SG(gameType);
        }

        public int SetSettleFlag_SG(int gameType, string referenceId)
        {
            return settledService.SetSettleFlag_SG(gameType, referenceId);
        }

        public int SettledFish_SG(SettleData_SG settleData_SG, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.SG, settleData_SG.Account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.SG, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate > 0)
            {
                settleData_SG.Stake *= tpCurrency.ExchangeRate;
                settleData_SG.WinLostAmount *= tpCurrency.ExchangeRate;
            }
            var result = settledService.SettledFish_SG(tpCustInfo, settleData_SG, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        #endregion

        public int CancelBetRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, bettingDataRNG_PP.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PP.amount *= tpCurrency.ExchangeRate;
            }

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

        public IEnumerable<BettingDataRNG_PP> GetUnsettledListRNG_PP()
        {
            return settledService.GetUnsettledListRNG_PP();
        }

        public int PromoBonus_PP(PromoBonus promoBonus, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PP, promoBonus.UserId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                promoBonus.Amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PromoBonus_PP(tpCustInfo, promoBonus, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                promoBonus.Amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public IEnumerable<UnsettleBettingDataLC_Allbet> GetUnsettledListLC_Allbet()
        {
            return settledService.GetUnsettledListLC_Allbet();
        }
        public int Get_TpWagerAllbet_Settle(long betNum, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            var result = settledService.Get_TpWagerAllbet_Settle(betNum, out stake, out validBet, out winLostAmount, out settleTime);
            return result;
        }
        public int SetSettleFlag_Allbet(long betNum)
        {
            return settledService.SetSettleFlag_Allbet(betNum);
        }
        public int SetSettleFlag_TpWagerAllbet(long betNum)
        {
            return settledService.SetSettleFlag_TpWagerAllbet(betNum);
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

            Thread.Sleep(5000);

            var result = settledService.CancelBetLC_Allbet(tpCustInfo, bettingDataLC_Allbet, out afterAmount);

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
        public int PayoutLC_Allbet(BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
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

            var result = settledService.PayoutLC_Allbet(tpCustInfo, bettingDataLC_Allbet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataLC_Allbet.Amount /= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount /= tpCurrency.ExchangeRate;
                }
            }

            return result;
        }
        public int ReSettledLC_Allbet(BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
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

            var result = settledService.ReSettledLC_Allbet(tpCustInfo, bettingDataLC_Allbet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataLC_Allbet.Amount /= tpCurrency.ExchangeRate;
                foreach (var bet in bettingDataLC_Allbet.Details)
                {
                    bet.Amount /= tpCurrency.ExchangeRate;
                }
            }

            return result;
        }

        public int SettledRNG_PG(BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.PG, bettingDataRNG_PG.PlayerName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_PG.TransferAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.SettledRNG_PG(tpCustInfo, bettingDataRNG_PG, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataRNG_PG.TransferAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        public int CancelBetLC_Sexy(BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, cancelBetTxn.userId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
            var result = settledService.CancelBetLC_Sexy(tpCustInfo, cancelBetTxn, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        public int SettledLC_Sexy(BettingDataLC_Sexy_Settle bettingDataLC_Sexy_Settle, out decimal afterAmount)
        {

            foreach (var txn in bettingDataLC_Sexy_Settle.Txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, txn.UserId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;

                if (tpCurrency.ExchangeRate > 0)
                {
                    txn.BetAmount *= tpCurrency.ExchangeRate;
                    txn.SettleWinLoss *= tpCurrency.ExchangeRate;
                    txn.ValidBet *= tpCurrency.ExchangeRate;
                    txn.WinLoss *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.SettledLC_Sexy(bettingDataLC_Sexy_Settle, out afterAmount);

            return result;
        }

        public int UnsettledLC_Sexy(Unsettle_VoidRound bettingDataLC_Sexy_Unsettle, out decimal afterAmount)
        {

            foreach (var txn in bettingDataLC_Sexy_Unsettle.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.UnsettledLC_Sexy(bettingDataLC_Sexy_Unsettle, out afterAmount);

            return result;
        }
        public int VoidRoundLC_Sexy(Unsettle_VoidRound voidRound, out decimal afterAmount)
        {

            foreach (var txn in voidRound.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.VoidRoundLC_Sexy(voidRound, out afterAmount);

            return result;
        }

        public int VoidTxnLC_Sexy(VoidTxn voidTxn, out decimal afterAmount)
        {

            foreach (var txn in voidTxn.txns)
            {

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.Sexy, txn.userId);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.Sexy, tpCustInfo.CurrencyId);
                txn.tpCustInfo = tpCustInfo;
            }

            var result = settledService.VoidTxnLC_Sexy(voidTxn, out afterAmount);

            return result;
        }

        #region EBet
        public int PayoutLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.totalBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.validBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.payout *= tpCurrency.ExchangeRate;
                foreach (var item in bettingData_EBet.detail.betList)
                {
                    item.betMoney *= tpCurrency.ExchangeRate;
                    item.payout *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetLC_EBet(BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.EBet, bettingData_EBet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingData_EBet.money *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.totalBet *= tpCurrency.ExchangeRate;
                bettingData_EBet.detail.validBet *= tpCurrency.ExchangeRate;
                foreach (var item in bettingData_EBet.detail.betList)
                {
                    item.betMoney *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.CancelBetLC_EBet(tpCustInfo, bettingData_EBet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RefundLC_EBet(RefundListEbet refundListEbet, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.EBet, refundListEbet.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                refundListEbet.refundMoney *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundLC_EBet(tpCustInfo, refundListEbet, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RefundBatchLC_EBet(BatchRefundList batchRefundList, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.EBet, batchRefundList.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                batchRefundList.refundMoney *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RefundBatchLC_EBet(tpCustInfo, batchRefundList, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleData_EBet> GetUnsettledListLC_EBet()
        {
            return settledService.GetUnsettledListLC_EBet();
        }

        public int SetSettleFlag_EBet(string TransId)
        {
            return settledService.SetSettleFlag_EBet(TransId);
        }

        public IEnumerable<CreditRecord_EBet> GetCreditRecordLC_EBet(string username, string seqNo)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.EBet, username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.EBet, tpCustInfo.CurrencyId);

            string[] list = seqNo.Split(',');
            var data = settledService.GetCreditRecordLC_EBet(list);
            foreach (var record in data)
            {
                record.money /= tpCurrency.ExchangeRate;
            }
            return data;
        }
        #endregion

        #region CQ9
        public int PayoutRNG_CQ9(EndRoundDataRNG_CQ9 endRoundDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, endRoundDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                foreach (var item in endRoundDataRNG_CQ9.detail)
                {
                    item.amount *= tpCurrency.ExchangeRate;
                }
            }

            var result = settledService.PayoutRNG_CQ9(tpCustInfo, endRoundDataRNG_CQ9, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RefundRNG_CQ9(RefundDataRNG_CQ9 refundDataRNG_CQ9, out decimal afterAmount, out string account)
        {
            account = "";
            afterAmount = 0;
            var data = settledService.GetRecordRNG_CQ9(refundDataRNG_CQ9.mtcode);
            if (data == null)
            {
                return -2;
            }
            if (data.TicketStatus == (int)TicketStatus.Refund)
            {
                return -3;
            }

            account = data.account;
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, account);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            var result = settledService.RefundRNG_CQ9(tpCustInfo, refundDataRNG_CQ9, data.amount, data.roundid, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int DebitRNG_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.DebitRNG_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CreditRNG_CQ9(BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, bettingDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CreditRNG_CQ9(tpCustInfo, bettingDataRNG_CQ9, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int RewardRNG_CQ9(PayoffDataRNG_CQ9 payoffDataRNG_CQ9, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, payoffDataRNG_CQ9.account);
            if (tpCustInfo == null)
            {
                afterAmount = 0;
                return -21;
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoffDataRNG_CQ9.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RewardRNG_CQ9(tpCustInfo, payoffDataRNG_CQ9, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public IEnumerable<SettleDataRNG_CQ9> GetUnsettledListRNG_CQ9()
        {
            return settledService.GetUnsettledListRNG_CQ9();
        }

        public int SetSettleFlagRNG_CQ9(string roundid, int SettleFlag)
        {
            return settledService.SetSettleFlagRNG_CQ9(roundid, SettleFlag);
        }

        public Record_CQ9 GetRecordRNG_CQ9(string mtcode)
        {
            var data = settledService.GetRecordRNG_CQ9(mtcode);

            if (data != null)
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.CQ9, data.account);
                var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.CQ9, tpCustInfo.CurrencyId);

                if (tpCurrency.ExchangeRate > 0)
                {
                    data.amount /= tpCurrency.ExchangeRate;
                    data.Before /= tpCurrency.ExchangeRate;
                    data.Balance /= tpCurrency.ExchangeRate;
                }
                data.currency = tpCurrency.TpCurrencyId;
            }
            return data;
        }

        #endregion

        #region WM

        public int PayoutLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelBetLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int ResettleLC_WM(BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.WM, bettingDataLC_WM.user);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.WM, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataLC_WM.money *= tpCurrency.ExchangeRate;
            }

            var result = settledService.ResettleLC_WM(tpCustInfo, bettingDataLC_WM, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleData_WM> GetUnsettledList_WM()
        {
            return settledService.GetUnsettledList_WM();
        }

        public SettleData_WM GetSettle_TpWagerWM(string TransId)
        {
            return settledService.GetSettle_TpWagerWM(TransId);
        }

        public int SetSettleFlag_TpWagerWM(string TransId)
        {
            return settledService.SetSettleFlag_TpWagerWM(TransId);
        }

        public int SetSettleFlag_BettransLC_WM(string TransId)
        {
            return settledService.SetSettleFlag_BettransLC_WM(TransId);
        }

        #endregion

    }
}
