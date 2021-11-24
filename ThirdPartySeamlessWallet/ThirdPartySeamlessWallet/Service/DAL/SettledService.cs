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
using Serilog.Context;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly Log2File log2File;
        private readonly ILogger<SettledService> logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly BettingService _bettingService;
        private readonly TpUnsettleService _tpUnsettleService;

        public SettledService(IServiceProvider provider)
        {
            _bettingService = provider.GetRequiredService<BettingService>();
            configuration = provider.GetRequiredService<IConfiguration>();
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            log2File = provider.GetRequiredService<Log2File>();
            logger = provider.GetRequiredService<ILogger<SettledService>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
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
            },
            {
                GameType.JILI_RNG, new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
                {
                    {"61",false },
                    {"62",false },
                    {"63",false },
                    {"64",false },
                    {"66",false },
                }
            }
        };

        int[] TransTimeTpIds = { (int)Tp.MP, (int)Tp.SA, (int)Tp.Allbet, (int)Tp.WM, (int)Tp.JILI, (int)Tp.YL, (int)Tp.EVO, (int)Tp.Netent, (int)Tp.RedTiger, (int)Tp.Sexy, (int)Tp.PT, (int)Tp.OneGaming, (int)Tp.NineW, (int)Tp.AMB, (int)Tp.TCG, (int)Tp.BTI, (int)Tp.One789, (int)Tp.V8, (int)Tp.KM, (int)Tp.EBet, (int)Tp.PP, (int)Tp.Iconic, (int)Tp.N2Live };

        private int PayoutV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, string betData, out decimal afterAmount, string memoHead, out int bonusId, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            afterAmount = 0;
            bonusId = 0;
            int result = 0;
            bool bHasBonusWallet = true;
            try
            {
                if (bHasBonusWallet)
                {
                    result = GetTpBonus(tpId, tpTransId, gameType, tpCategory, out int custId, out bonusId);

                    if (bonusId > 0 || transType == TransType.Prize)
                    {
                        if (bonusId > 0 && custId != tpCustInfo.CustId)
                        {
                            return 0;
                        }

                        result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out decimal afterBonus, ref bonusId, conn, trans);

                        //var bonusMinBet = thirdPartyBLL.GetBonusMinBet(gameType, tpCustInfo.CurrencyId);

                        if ((transType == TransType.Prize && bonusId == 0) || afterBonus < 0 || bonusId == -1)
                        {
                            result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, afterBonus, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                        }
                        else
                        {
                            afterAmount = afterBonus + walletService.GetBalance(tpCustInfo.CustId);
                        }

                        if (afterBonus > 0)
                        {
                            // check bonus status and update status cancel to running
                            AutoRunningSeamlessBonus(bonusId, tpCustInfo.CurrencyId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, afterBonus, conn, trans);
                        }
                    }
                    else
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                }
            }
            catch (Exception ex)
            {
                result = 0;
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    amount = amount,
                    tpId = tpId,
                    tpTransId = tpTransId,
                    gameType = gameType,
                    tpCategory = tpCategory,
                    refId = refId,
                    transType = transType,
                    betData = betData,
                    afterAmount = afterAmount,
                    memoHead = memoHead,
                    bonusId = bonusId,
                    allowNegative = allowNegative
                };
                logger.LogError(ex, $"PARAMETER: {parameter}, EXCEPTION: {ex.Message}");
                log2File.Error($"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}", MethodBase.GetCurrentMethod().Name);
            }

            return result;
        }

        private int PayoutWithBonusId(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, out decimal afterAmount, string memoHead, int bonusId, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            afterAmount = 0;
            int result = 0;
            try
            {
                result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out decimal afterBonus, ref bonusId, conn, trans);

                if (afterBonus < 0 || bonusId == -1)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, afterBonus, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                }
                else
                {
                    afterAmount = afterBonus + walletService.GetBalance(tpCustInfo.CustId);
                }

                if (afterBonus > 0)
                {
                    // check bonus status and update status cancel to running
                    AutoRunningSeamlessBonus(bonusId, tpCustInfo.CurrencyId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, afterBonus, conn, trans);
                }
            }
            catch (Exception ex)
            {
                result = 0;
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    amount = amount,
                    tpId = tpId,
                    tpTransId = tpTransId,
                    gameType = gameType,
                    tpCategory = tpCategory,
                    refId = refId,
                    transType = transType,
                    afterAmount = afterAmount,
                    memoHead = memoHead,
                    bonusId = bonusId,
                    allowNegative = allowNegative
                };
                logger.LogError(ex, $"PARAMETER: {parameter}, EXCEPTION: {ex.Message}");
                log2File.Error($"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}", MethodBase.GetCurrentMethod().Name);
            }

            return result;
        }

        private int GetTpBonus(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId)
        {
            CustId = 0;
            BonusId = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { TpId = tpId, TransId = tpTransId, GameType = gameType });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute($"Service_Tp{tpCategory}Bonus_Get", p, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                CustId = p.Get<int>("CustId");
                BonusId = p.Get<int>("BonusId");
            }

            return result;
        }

        public bool GetBonusIdByPromoCategoryId(int custId, int promoCategoryId, out int bonusId)
        {
            bonusId = 0;
            var p = new DynamicParameters(new { CustId = custId, PromoCategoryId = promoCategoryId });
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BonusIdByPromoCategoryId_Get", CommandType.StoredProcedure, p);
            if (result > 0)
            {
                bonusId = p.Get<int?>("BonusId") ?? 0;
            }

            return result > 0 && bonusId != 0;
        }

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
            if (bonusId == 0 && betRecordInfo.TpId == (int)Tp.N2Live && !string.IsNullOrEmpty(betRecordInfo.ReferenceNo))
            {
                // for n2 smart bet use
                // for n2 temporary fix noraml bet
                GetTpBonus(betRecordInfo.TpId, betRecordInfo.ReferenceNo, gameType, tpCategory, out custId, out bonusId);
            }

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

            try
            {
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
                            {
                                if (betRecord.TpId == (int)Tp.PT)
                                    AutoCancelTransferBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, conn, trans);
                                else
                                    AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);

                            }
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
                            {
                                if (RefBetRecord.TpId == (int)Tp.PT)
                                    AutoCancelTransferBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, conn, trans);
                                else
                                    AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);
                            }
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
                                {
                                    if (RefBetRecord.TpId == (int)Tp.PT)
                                        AutoCancelTransferBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, conn, trans);
                                    else
                                        AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, conn, trans);
                                }
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
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                LogContext.PushProperty("TpId", betRecord.TpId);
                LogContext.PushProperty("GameType", betRecord.GameType);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    betRecord = JsonConvert.SerializeObject(betRecord),
                    backAmount = backAmount,
                    afterAmount = afterAmount,
                    doPayout = doPayout
                };
                logger.LogError(ex, $"PARAMETER: {parameter}, EXCEPTION: {ex.Message}");
                log2File.Error($"{JsonConvert.SerializeObject(parameter)}\r\n{ex.Message}", MethodBase.GetCurrentMethod().Name);
                throw;
            }
            return result;
        }
        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.QueryFirstOrDefault<BetRecordInfo>("Service_TpBetRecord_Get", new { TpId = TpId, TransId = TransId, GameType = GameType }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            //  temporary condition for TransTime
            if (TransTimeTpIds.Contains(betRecordInfo.TpId))
            {
                return conn.Execute("Service_TpSettleBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, TransTime = betRecordInfo.TransTime, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, ProgressiveBet = betRecordInfo.ProgressiveBet, ProgressiveWin = betRecordInfo.ProgressiveWin, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            return conn.Execute("Service_TpSettleBet_Set_V2", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, ProgressiveBet = betRecordInfo.ProgressiveBet, ProgressiveWin = betRecordInfo.ProgressiveWin, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            //  temporary condition for TransTime
            if (TransTimeTpIds.Contains(betRecordInfo.TpId))
            {
                return conn.Execute("Service_TpSettleBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, TransTime = betRecordInfo.TransTime, Stake = betRecordInfo.Stake, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, ProgressiveBet = betRecordInfo.ProgressiveBet, ProgressiveWin = betRecordInfo.ProgressiveWin, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus, BonusId = betRecordInfo.BonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            return conn.Execute("Service_TpSettleBet_Ins_V2", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, ValidBet = betRecordInfo.ValidBet, Revenue = betRecordInfo.Revenue, ProgressiveBet = betRecordInfo.ProgressiveBet, ProgressiveWin = betRecordInfo.ProgressiveWin, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus, BonusId = betRecordInfo.BonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        private int AutoCancelTransferBonus(int bonusId, int currencyId, int gameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelTransferBonus_Set", new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType }, trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelSeamlessBonus(int bonusId, int currencyId, int gameType, int custId, string userName, int siteId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType, CustId = custId, UserName = userName, SiteId = siteId });
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var result = conn.Execute("Service_AutoCancelSeamlessBonusV2_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            decimal bonus = p.Get<decimal?>("Bonus") ?? 0;

            if (bonus > 0)
            {
                result = walletService.SetBalance(custId, siteId, userName, bonus, 0, TransType.Prize, $"Bonus: {Math.Abs(bonus).ToString("#,##0.##")}", bonusId.ToString(), out _, conn, trans);
            }

            return result;
        }

        public int AutoRunningSeamlessBonus(int bonusId, int currencyId, int gameType, int custId, string userName, int siteId, decimal afterBonus, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType, CustId = custId, UserName = userName, SiteId = siteId, AfterBonus = afterBonus });
            p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute("Service_AutoRunningSeamlessBonus_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            int outResult = p.Get<int>("out_result");
            if (outResult == 2)
            {
                var parameter = new
                {
                    BonusId = bonusId,
                    GameType = gameType,
                    CustId = custId,
                    AfterBonus = afterBonus,
                    OutResult = outResult
                };
                log2File.Info($"{JsonConvert.SerializeObject(parameter)}", MethodBase.GetCurrentMethod().Name);
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
