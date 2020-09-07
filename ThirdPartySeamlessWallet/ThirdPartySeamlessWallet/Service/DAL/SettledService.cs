using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using System.Linq;
using System.Collections.Generic;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly BettingService bettingService;
        private readonly Log2File log2File;
        public SettledService(IConfiguration configuration, WalletService walletService, Tools tools, BettingService bettingService, Log2File log2File)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
            this.bettingService = bettingService;
            this.log2File = log2File;
        }
        public int Settle(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int SettledFish_YL(TpCustInfo tpCustInfo, BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            long transTime = 0;
            DateTime.TryParse(bettingDataFish_YL.transTime, out DateTime dtTransTime);
            transTime = tools.ConvertLocTime(dtTransTime, 8);
            try
            {
                result = conn.Execute("Service_BettransFish_YL_Ins", new { bettingDataFish_YL.txId, bettingDataFish_YL.userId, gameNumier = bettingDataFish_YL.gameNumber, bettingDataFish_YL.gameId, bettingDataFish_YL.chair, bettingDataFish_YL.roomId, bettingDataFish_YL.roomLevel, bettingDataFish_YL.currency, transTime, bettingDataFish_YL.betAmount, bettingDataFish_YL.payAmount, bettingDataFish_YL.commission, bettingDataFish_YL.profit, bettingDataFish_YL.validBet }, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataFish_YL.profit != 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataFish_YL.profit, (int)Tp.YL, TransType.Settled, $"Settled: {Math.Abs(bettingDataFish_YL.profit).ToString("#,##0.##")}", bettingDataFish_YL.txId, out afterAmount, conn, trans);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.YL;
                        betRecordInfo.Stake = bettingDataFish_YL.validBet;
                        betRecordInfo.WinlostAmount = bettingDataFish_YL.profit;
                        betRecordInfo.TransId = bettingDataFish_YL.txId;
                        betRecordInfo.GameType = GameType.YL_Fish;
                        betRecordInfo.TpUserName = bettingDataFish_YL.userId;
                        betRecordInfo.SettledTime = 0;
                        betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }
                else
                {
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }
        public int RefundFish_YL(TpCustInfo tpCustInfo, RefundDataFish_YL refundDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = 0;
            long updateTime = 0;
            DateTime.TryParse(refundDataFish_YL.updateTime, out DateTime dtUpdateTime);
            updateTime = tools.ConvertLocTime(dtUpdateTime, 8);
            BettingDataFish_YL bettingDataFish_YL = conn.QueryFirstOrDefault<BettingDataFish_YL>("Service_BettransFish_YL_Get", new { refundDataFish_YL.txId }, transaction: trans, commandType: CommandType.StoredProcedure);

            if (bettingDataFish_YL != null && bettingDataFish_YL.TicketStatus != TicketStatus.Refund)
            {
                if (bettingDataFish_YL.profit != 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataFish_YL.profit * -1, (int)Tp.YL, TransType.Refunds, $"Void: {bettingDataFish_YL.profit.ToString("#,##0.##")}", bettingDataFish_YL.txId, out afterAmount, conn, trans);
                }

                if (result > 0)
                {
                    result = conn.Execute("Service_BettransFish_YL_Set", new { refundDataFish_YL.txId, updateTime = updateTime }, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.YL;
                        betRecordInfo.Stake = bettingDataFish_YL.validBet;
                        betRecordInfo.WinlostAmount = bettingDataFish_YL.profit;
                        betRecordInfo.TransId = bettingDataFish_YL.txId;
                        betRecordInfo.GameType = GameType.YL_Fish;
                        betRecordInfo.TpUserName = bettingDataFish_YL.userId;
                        betRecordInfo.SettledTime = updateTime;
                        betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        betRecordInfo.TicketStatus = TicketStatus.Refund;

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }

                if (result > 0 && bettingDataFish_YL.profit == 0)
                {
                    result = -1;
                }
            }
            else
            {
                result = -1;
            }

            return result;
        }
        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, decimal backAmount, out decimal afterAmount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecord, backAmount, out afterAmount, conn, trans);
            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, decimal backAmount, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans, bool doPayout = true)
        {
            int result = 0;
            afterAmount = 0;
            var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);
            #region Payout Process
            Enum.TryParse(betRecord.TpId.ToString(), out Tp tp);

            if (doPayout && RefBetRecord == null)
            {
                return -1;
            }
            else if (doPayout && RefBetRecord.TicketStatus != TicketStatus.UnSettled)
            {
                return -1;
            }
            else
            {
                if (betRecord.Stake == 0 && RefBetRecord != null)
                {
                    betRecord.Stake = RefBetRecord.Stake;
                }

                if (betRecord.TicketStatus == TicketStatus.Lost && doPayout && backAmount == 0)
                {
                    betRecord.WinlostAmount = -betRecord.Stake;
                }
                else if (betRecord.TicketStatus != TicketStatus.Refund && betRecord.SettlementType != SettlementType.UnSettled)
                {
                    if (doPayout)
                    {
                        betRecord.WinlostAmount = backAmount - betRecord.Stake;
                    }

                    if (betRecord.WinlostAmount > 0)
                    {
                        if (betRecord.Stake != 0)
                        {
                            betRecord.TicketStatus = TicketStatus.Win;
                        }
                        else
                        {
                            betRecord.TicketStatus = TicketStatus.FreeBetWin;
                        }
                    }
                    else if (betRecord.WinlostAmount == 0)
                    {
                        betRecord.TicketStatus = TicketStatus.Draw;
                    }
                    else
                    {
                        betRecord.TicketStatus = TicketStatus.Lost;
                    }
                }

                if (backAmount > 0 && doPayout)
                {
                    if (betRecord.TicketStatus == TicketStatus.Refund)
                    {
                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Refunds, $"Refund: {backAmount}", betRecord.TransId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        string sMemo = $"Payout: {backAmount.ToString("#,##0.##")}";
                        if (betRecord.TicketStatus == TicketStatus.FreeBetWin)
                        {
                            sMemo = $"Free Bet Payout: {backAmount.ToString("#,##0.##")}";
                        }

                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Settled, sMemo, betRecord.TransId, out afterAmount, conn, trans);
                    }
                }
            }

            #endregion
            decimal validBet = betRecord.ValidBet ?? betRecord.Stake;
            bool hasBonusRunning = false;

            if (RefBetRecord != null && RefBetRecord.BonusId > 0)
            {
                betRecord.BonusId = RefBetRecord.BonusId;
            }

            int bonusId = betRecord.BonusId;
            #region Set TurnOver of RollOver
            if (RefBetRecord == null)
            {
                if (betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans) > 0;
                }
            }
            else
            {
                decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", RefBetRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", betRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && refValidBet != validBet)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans) > 0;
                }
            }
            #endregion

            #region Set TurnOver of WithdrawalLimit
            if (!hasBonusRunning)
            {
                if (RefBetRecord == null)
                {
                    if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                }
                else
                {
                    decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                    if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                    else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled))
                    {
                        SetWithdrawalLimitTurnOver(RefBetRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
                    }
                    else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Draw)
                        && refValidBet != validBet)
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    }
                }
            }
            #endregion

            #region Set WinLostAmount of Cust
            decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
            decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;
            if (RefBetRecord == null && betRecord.WinlostAmount != 0)
            {
                if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin)
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        if (hasBonusRunning)
                            AutoCancelSeamlessBonus(bonusId, conn, trans);
                        else
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                    }
                }
            }
            else if (RefBetRecord != null)
            {
                decimal refWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                decimal refLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        if (hasBonusRunning)
                            AutoCancelSeamlessBonus(bonusId, conn, trans);
                        else
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                    }
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", betRecord.TransId, conn, trans);

                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", bonusId, betRecord.TransId, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    if (refWinnings != Winnings || refLosses != Losses)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", betRecord.TransId, conn, trans);

                        if (hasBonusRunning)
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", bonusId, betRecord.TransId, conn, trans);

                        if (Losses != 0)
                        {
                            if (hasBonusRunning)
                                AutoCancelSeamlessBonus(bonusId, conn, trans);
                            else
                                AutoCancelBonus(tpCustInfo.CustId, conn, trans);

                        }
                    }
                }
            }
            #endregion

            #region Set EffectiveBet of Cust
            if (RefBetRecord == null)
            {
                if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet, conn, trans);
                }
            }
            else
            {
                decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, refValidBet * -1, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    if (refValidBet != validBet)
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet - refValidBet, conn, trans);
                    }
                }
            }
            #endregion

            if (RefBetRecord == null)
            {
                result = AddTpBetRecord(betRecord, tpCustInfo, conn, trans);
            }
            else
            {
                result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
            }

            return result;
        }
        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.QueryFirstOrDefault<BetRecordInfo>("Service_TpBetRecord_Get", new { TpId = TpId, TransId = TransId, GameType = GameType }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, ValidBet = betRecordInfo.ValidBet, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, ValidBet = betRecordInfo.ValidBet, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;
            return conn.Execute("Common_RollOverTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int gameType, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;
            return conn.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, GameType = gameType, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;
            return conn.Execute("Common_WinLostAmount_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, MySqlConnection conn, MySqlTransaction trans)
        {
            if (validBet == 0)
                return 0;
            return conn.Execute("Service_EffectiveBetSum_Set", new { CustId = CustId, SiteId = SiteId, Stake = validBet }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelBonus(int CustId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelBonus_Set", new { CustId = CustId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelSeamlessBonus(int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelSeamlessBonus_Set", new { BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(ref int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var p = new DynamicParameters(new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Turnover = Turnover, Memo, RefTransId });
            p.Add("BonusId", value: BonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            var result = conn.Execute("Service_RolloverTurnover_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            BonusId = p.Get<int>("BonusId");
            return BonusId > 0 ? 1 : 0;
        }
        public int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            return conn.Execute("Service_BonusWinLost_Set", new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, BonusId = BonusId, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<dynamic> GetTpBetRecordUnsettle(int tpId, int gameType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<dynamic>("Service_TpBetRecordUnsettle_Get", new { TpId = tpId, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }
    }
}
