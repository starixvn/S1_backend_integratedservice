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
        private readonly Log2File log2File;
        public SettledService(IConfiguration configuration, WalletService walletService, Tools tools, Log2File log2File)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
            this.log2File = log2File;
        }

        private Dictionary<GameType, Dictionary<string, bool>> excludeRoll = new Dictionary<GameType, Dictionary<string, bool>>()
        {
            {
                GameType.Joker_RNG, new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    {"dc7sh3dfmjpio",false },
                    {"3uim5ppkiqwk1",false },
                    {"j3wngk3efrzn6",false },
                    {"yr1zy9u9xt6zr",false },
                    {"hxb3p8r71kj3y",false },
                }
            },
            {
                GameType.PG_RNG, new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    {"10",false },
                    {"12",false },
                    {"11",false },
                    {"31",false },
                    {"76",false },
                    {"77",false },
                    {"78",false },
                }
            },
            {
                GameType.PP_RNG, new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    {"bndt",false },
                    {"bnadvanced",false },
                    {"bca",false },
                    {"bjmb",false },
                    {"bjma",false },
                    {"rla",false },
                }
            }
        };
        public SettlementType GetSettlementType(GameType gameType, string gameId)
        {
            SettlementType settlementType = SettlementType.SettledForValid;

            if (excludeRoll.ContainsKey(gameType))
            {
                if (excludeRoll[gameType].ContainsKey(gameId))
                {
                    settlementType = SettlementType.SettledForValidNotRollover;
                }
            }

            return settlementType;
        }
        public int Settle(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, TpCategory tpCategory)
        {
            int bonusId = 0;
            int gameType = (int)betRecordInfo.GameType;

            GetTpBonus(betRecordInfo.TpId, betRecordInfo.TransId, gameType, tpCategory, out int custId, out bonusId);
            betRecordInfo.BonusId = bonusId;

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

                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Payout, sMemo, betRecord.TransId, out afterAmount, conn, trans);
                    }
                }
            }

            #endregion
            decimal validBet = betRecord.ValidBet ?? betRecord.Stake;

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
                    //SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                }
            }
            else
            {
                decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    //SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                {
                    //SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", RefBetRecord.TransId, conn, trans);
                    SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", betRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                {
                    //SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                    SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && refValidBet != validBet)
                {
                    //SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                }
            }
            #endregion

            #region Set TurnOver of WithdrawalLimit
            if (bonusId == 0)
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
                    if (bonusId > 0)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        if (bonusId > 0)
                            AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);
                        /*else
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);*/
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
                    if (bonusId > 0)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        if (bonusId > 0)
                            AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);
                        /*else
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);*/
                    }
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", betRecord.TransId, conn, trans);

                    if (bonusId > 0)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", bonusId, betRecord.TransId, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    if (refWinnings != Winnings || refLosses != Losses)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", betRecord.TransId, conn, trans);

                        if (bonusId > 0)
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", bonusId, betRecord.TransId, conn, trans);

                        if (Losses != 0)
                        {
                            if (bonusId > 0)
                                AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);
                            /*else
                                AutoCancelBonus(tpCustInfo.CustId, conn, trans);*/
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
            return conn.Execute("Service_TpSettleBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus, BonusId = betRecordInfo.BonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        public int AutoCancelSeamlessBonus(int bonusId, int currencyId, int gameType, int custId, string userName, int siteId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType, CustId = custId, UserName = userName, SiteId = siteId });
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var result = conn.Execute("Service_AutoCancelSeamlessBonus_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            decimal bonus = p.Get<decimal?>("Bonus") ?? 0;

            if (bonus > 0)
            {
                result = walletService.SetBalance(custId, siteId, userName, bonus, 0, TransType.Prize, $"Bonus: {Math.Abs(bonus).ToString("#,##0.##")}", bonusId.ToString(), out _, conn, trans);
            }

            return result;
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
