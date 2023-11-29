using System.Data;
using System.Reflection;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly Log2File log2File;
        private readonly ILogger<SettledService> logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly BettingService _bettingService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly TpUnRewardService _tpUnRewardService;
        private readonly PartitionService _partitionService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpBetRecordExtendBLL _tpBetRecordExtendBLL;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly IHostEnvironment _environment;

        public SettledService(IServiceProvider provider)
        {
            _bettingService = provider.GetRequiredService<BettingService>();
            walletService = provider.GetRequiredService<WalletService>();
            tools = provider.GetRequiredService<Tools>();
            log2File = provider.GetRequiredService<Log2File>();
            logger = provider.GetRequiredService<ILogger<SettledService>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpBetRecordExtendBLL = provider.GetRequiredService<TpBetRecordExtendBLL>();
            _tpUnRewardService = provider.GetRequiredService<TpUnRewardService>();
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
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

        public int PayoutV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, string betData, out decimal afterAmount, string memoHead, out int bonusId, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            afterAmount = 0;
            bonusId = 0;
            int result;
            try
            {
                result = GetTpBonus(tpId, tpTransId, gameType, tpCategory, out var custId, out bonusId);

                if (bonusId > 0 || transType == TransType.Prize)
                {
                    if (bonusId > 0 && custId != tpCustInfo.CustId)
                    {
                        return 0;
                    }

                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount):#,##0.##}", refId, out var afterBonus, ref bonusId, conn, trans);

                    if ((transType == TransType.Prize && bonusId == 0) || afterBonus < 0 || bonusId == -1)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, afterBonus, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus):#,##0.##}", refId, out afterAmount, conn, trans, allowNegative);
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
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount):#,##0.##}", refId, out afterAmount, conn, trans, allowNegative);
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                var parameter = new
                {
                    tpCustInfo = JsonConvert.SerializeObject(tpCustInfo),
                    amount,
                    tpId,
                    tpTransId,
                    gameType,
                    tpCategory,
                    refId,
                    transType,
                    betData,
                    afterAmount,
                    memoHead,
                    bonusId,
                    allowNegative
                };
                logger.LogError(ex, $"PARAMETER: {parameter}, EXCEPTION: {ex.Message}");
                throw;
            }

            return result;
        }

        public int PayoutWithBonusId(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, out decimal afterAmount, string memoHead, int bonusId, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
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
                throw;
            }

            return result;
        }

        public int GetTpBonus(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId, GetTpSeamlessBonusType getTpSeamlessBonusType = GetTpSeamlessBonusType.ByGameTypeAndTransId)
        {
            return GetTpSeamlessBonus(tpId, tpTransId, gameType, out CustId, out BonusId, getTpSeamlessBonusType);
        }

        private int GetTpSeamlessBonus(int TpId, string TransId, int GameType, out int CustId, out int BonusId, GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            CustId = 0;
            BonusId = 0;
            var spName = getTpSeamlessBonusType == GetTpSeamlessBonusType.ByGameTypeAndTransId ?
                "Service_TpSeamlessBonus_Get" : "Service_TpSeamlessBonus_GetByTransID";
            var p = new DynamicParameters(new { TpId, TransId, GameType });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute(spName, parameters: p);
            if (result > 0)
            {
                CustId = p.Get<int>("CustId");
                BonusId = p.Get<int>("BonusId");
            }

            return result;
        }

        private (int BonusId, decimal UseBonus) GetUseBonus(int TpId, string ReferenceNo, int GameType, GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            var spName = getTpSeamlessBonusType == GetTpSeamlessBonusType.ByGameTypeAndTransId ?
               "Service_TpSeamlessBonus_UseBonus_Get" : "Service_TpSeamlessBonus_UseBonus_GetByTransID";
            var p = new DynamicParameters(new { TpId, TransId = ReferenceNo, GameType });
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("UseBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute(spName, parameters: p);

            if (result > 0)
            {
                return (p.Get<int>("BonusId"), p.Get<decimal>("UseBonus"));
            }

            return default;
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

        public int Settle(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            int bonusId = 0;
            int gameType = (int)betRecordInfo.GameType;

            // 1.先用TransId去TpSeamlessBonus取得BonusId
            // 2.取不到改用ReferenceNo再去TpSeamlessBonus檢查一次
            GetTpSeamlessBonus(betRecordInfo.TpId, betRecordInfo.TransId, gameType, out int custId, out bonusId, getTpSeamlessBonusType);
            if (bonusId == 0 && !string.IsNullOrEmpty(betRecordInfo.ReferenceNo))
            {
                GetTpSeamlessBonus(betRecordInfo.TpId, betRecordInfo.ReferenceNo, gameType, out custId, out bonusId, getTpSeamlessBonusType);
            }

            betRecordInfo.BonusId = bonusId;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecordInfo, conn, trans, getTpSeamlessBonusType);

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return result;
        }

        public int Settle(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecordInfo, conn, trans);

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;
        }

        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, MySqlConnection conn, MySqlTransaction trans, GetTpSeamlessBonusType getTpSeamlessBonusType = GetTpSeamlessBonusType.ByGameTypeAndTransId)
        {
            int result = 0;
            try
            {
                // 取得第三方錢包類型
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartyBySite(betRecord.TpId, tpCustInfo.SiteId);
                // 取得前一次的結算結果
                var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);

                // 指定走新結算流程
                if (_environment.IsDevelopment() || tpCustInfo.CustId == 22)
                {
                    var settleTicket = new SettleTicket(
                        tpCustInfo: tpCustInfo,
                        betRecord: betRecord,
                        refBetRecord: RefBetRecord,
                        mainDbClient: _mainDbClient,
                        trans: trans,
                        walletService: walletService,
                        logger: logger,
                        tpBetRecordExtendBLL: _tpBetRecordExtendBLL,
                        thirdPartyBLL: _thirdPartyBLL
                        );

                    settleTicket.ExcuteSettleTicket(thirdPartySetting.WalletType, getTpSeamlessBonusType);
                    return settleTicket.SettleResult ? 1 : 0;
                }

                #region 設定初始狀態

                Enum.TryParse(betRecord.TpId.ToString(), out Tp tp);

                if (betRecord.Stake == 0 && RefBetRecord != null)
                {
                    betRecord.Stake = RefBetRecord.Stake;
                }

                if (betRecord.TicketStatus != TicketStatus.Refund && betRecord.SettlementType != SettlementType.UnSettled)
                {
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

                decimal validBet = betRecord.ValidBet ?? betRecord.Stake;

                if (RefBetRecord != null && RefBetRecord.BonusId > 0)
                {
                    betRecord.BonusId = RefBetRecord.BonusId;
                }

                int bonusId = betRecord.BonusId;

                #endregion

                #region 更新玩家輸贏總金額
                decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

                if (RefBetRecord == null && betRecord.WinlostAmount != 0)
                {
                    if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);

                        if (bonusId > 0)
                        {
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);
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
                        {
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);
                        }
                    }
                    else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                        && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", betRecord.TransId, conn, trans);

                        if (bonusId > 0)
                        {
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", bonusId, betRecord.TransId, conn, trans);
                        }
                    }
                    else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                    {
                        if (refWinnings != Winnings || refLosses != Losses)
                        {
                            SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", betRecord.TransId, conn, trans);

                            if (bonusId > 0)
                            {
                                SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", bonusId, betRecord.TransId, conn, trans);
                            }
                        }
                    }
                }

                // 把AutoCancel移出來，只要此次注單狀態不是Unsettle都會執行AutoCancel檢查
                if (bonusId > 0 && betRecord.TicketStatus != TicketStatus.UnSettled)
                {
                    var autoCancelResult = thirdPartySetting.WalletType switch
                    {
                        WalletType.TransferWallet => AutoCancelTransferBonus(bonusId, betRecord.TransId, tpCustInfo.CurrencyId, (int)betRecord.GameType, conn, trans),
                        WalletType.SeamlessWallet => AutoCancelSeamlessBonus(bonusId, tpCustInfo.CurrencyId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.TransId, conn, trans),
                        _ => throw new NotImplementedException()
                    };                    
                }

                #endregion

                #region 有Bonus將流水沖銷玩家Bonus的Rollover

                if (RefBetRecord == null)
                {
                    if (betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                }
                else
                {
                    decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                    if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                    {
                        SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", betRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && refValidBet != validBet)
                    {
                        SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    }
                }

                #endregion

                #region 沒Bonus將流水沖銷玩家的提款限額

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
                else
                {
                    // 使用Bonus的要判斷是否有用到主錢包下注，主錢包下注的金額，第一次Settle時要沖提款限額
                    var referenceNo = string.IsNullOrEmpty(betRecord.ReferenceNo) ? betRecord.TransId : betRecord.ReferenceNo;
                    var (_, useBonus) = GetUseBonus(betRecord.TpId, referenceNo, (int)betRecord.GameType, getTpSeamlessBonusType);
                    var stakeWithoutBonus = validBet - useBonus; // 沒有使用Bonus用主錢包下注的下注的金額
                    if (stakeWithoutBonus > 0 && (RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled))
                    {
                        if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                        {
                            SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, stakeWithoutBonus, "Settled", betRecord.TransId, conn, trans);
                        }
                    }
                }
                #endregion

                #region 更新玩家有效下注總金額

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

                try
                {
                    _ = Task.Run(() => 
                    { 
                        _ = _tpBetRecordExtendBLL.AddTpBetRecordExtend(betRecord, tpCustInfo); 
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"{ex.Message} || {JsonConvert.SerializeObject(betRecord)}");
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("Observe", MethodBase.GetCurrentMethod().Name);
                LogContext.PushProperty("TpId", betRecord.TpId);
                LogContext.PushProperty("GameType", betRecord.GameType);
                var parameter = new
                {
                    tpCustInfo,
                    betRecord,
                };
                logger.LogError(ex, $"PARAMETER: {JsonConvert.SerializeObject(parameter)}");
                throw;
            }
            return result;
        }

        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", trans, new { TpId = TpId, TransId = TransId, GameType = GameType });
        }

        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_TpSettleBet_Set_V4", new
            {
                betRecordInfo.TpId,
                betRecordInfo.TransId,
                GameType = (int)betRecordInfo.GameType,
                tpCustInfo.CustId,
                tpCustInfo.UserName,
                tpCustInfo.CurrencyId,
                tpCustInfo.SiteId,
                betRecordInfo.TpUserName,
                betRecordInfo.SettledTime,
                betRecordInfo.TransTime,
                betRecordInfo.Stake,
                betRecordInfo.ValidBet,
                betRecordInfo.Revenue,
                betRecordInfo.ProgressiveBet,
                betRecordInfo.ProgressiveWin,
                betRecordInfo.WinlostAmount,
                SettlementType = (int)betRecordInfo.SettlementType,
                TicketStatus = (int)betRecordInfo.TicketStatus,
                betRecordInfo.BonusId,
                betRecordInfo.GameID
            }, conn, trans);
        }

        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {

            return _mainDbClient.Execute("Service_TpSettleBet_Ins_V2", new
            {
                betRecordInfo.TpId,
                betRecordInfo.TransId,
                GameType = (int)betRecordInfo.GameType,
                tpCustInfo.CustId,
                tpCustInfo.UserName,
                tpCustInfo.CurrencyId,
                tpCustInfo.SiteId,
                betRecordInfo.TpUserName,
                betRecordInfo.SettledTime,
                betRecordInfo.TransTime,
                betRecordInfo.Stake,
                betRecordInfo.ValidBet,
                betRecordInfo.Revenue,
                betRecordInfo.ProgressiveBet,
                betRecordInfo.ProgressiveWin,
                betRecordInfo.WinlostAmount,
                SettlementType = (int)betRecordInfo.SettlementType,
                TicketStatus = (int)betRecordInfo.TicketStatus,
                betRecordInfo.BonusId,
                betRecordInfo.GameID
            }, conn, trans);
        }

        /// <summary>
        /// 使用流水沖銷玩家的提款限額
        /// </summary>
        public int SetWithdrawalLimitTurnOver(int TpId, int gameType, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
            {
                return 0;
            }
            
            return _mainDbClient.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, GameType = gameType, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, conn, trans);
        }

        /// <summary>
        /// 更新玩家輸贏總金額
        /// </summary>
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
            {
                return 0;
            }
            
            return _mainDbClient.Execute("Common_WinLostAmount_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, RefTransId = RefTransId }, conn, trans);
        }

        /// <summary>
        /// 更新玩家有效下注總金額
        /// </summary>
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, MySqlConnection conn, MySqlTransaction trans)
        {
            if (validBet == 0)
            {
                return 0;
            }
            
            return _mainDbClient.Execute("Service_EffectiveBetSum_Set", new { CustId = CustId, SiteId = SiteId, Stake = validBet }, conn, trans);
        }

        private int AutoCancelTransferBonus(int bonusId, string transId, int currencyId, int gameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_AutoCancelTransferBonus_Set_V2", new { BonusId = bonusId, TransId = transId, CurrencyId = currencyId, GameType = gameType }, conn, trans);
        }

        private int AutoCancelSeamlessBonus(int bonusId, int currencyId, int gameType, int custId, string userName, int siteId, string transId, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType, CustId = custId, UserName = userName, SiteId = siteId, TransId = transId });            
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("BonusStatus", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_AutoCancelSeamlessBonus_Set_V4", p, conn, trans);
            var bonus = p.Get<decimal?>("Bonus") ?? 0;
            var bonusStatus = p.Get<int?>("BonusStatus") ?? 0;
            
            if (bonusStatus == 2)
            {
                logger.LogWarning($"AutoCancelSeamlessBonus Success: BonusId={bonusId}, TransId={transId}, bonus={bonus}");
                if (bonus > 0)
                {
                    result = walletService.SetBalance(custId, siteId, userName, bonus, 0, TransType.Prize, $"Bonus: {Math.Abs(bonus):#,##0.##}", bonusId.ToString(), out _, conn, trans);
                }                
            }

            return result;
        }

        public int AutoRunningSeamlessBonus(int bonusId, int currencyId, int gameType, int custId, string userName, int siteId, decimal afterBonus, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new { BonusId = bonusId, CurrencyId = currencyId, GameType = gameType, CustId = custId, UserName = userName, SiteId = siteId, AfterBonus = afterBonus });
            p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("CancelBonusAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_AutoRunningSeamlessBonus_Set_V3", p, conn, trans);
            int outResult = p.Get<int>("out_result");
            var runningAfterBonus = p.Get<decimal>("CancelBonusAmount");
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
                logger.LogInformation($"{JsonConvert.SerializeObject(parameter)}");

                if (runningAfterBonus > 0) //將退回主帳戶的餘額重新扣掉
                {
                    var amount = runningAfterBonus * -1;
                    result = walletService.SetBalance(custId, siteId, userName, amount, 0, TransType.Prize, $"Bonus: {Math.Abs(amount).ToString("#,##0.##")}", bonusId.ToString(), out _, conn, trans, AllowNegative.Allow);
                }
            }
            return result;
        }

        /// <summary>
        /// 使用流水沖銷玩家Bonus的Rollover
        /// </summary>
        public int SetRollOverTurnOver(ref int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var p = new DynamicParameters(new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Turnover = Turnover, Memo, RefTransId });
            p.Add("BonusId", value: BonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            var result = _mainDbClient.Execute("Service_RolloverTurnover_Set_V2", p, conn, trans);
            BonusId = p.Get<int>("BonusId");
            return BonusId > 0 ? 1 : 0;
        }

        public int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            return _mainDbClient.Execute("Service_BonusWinLost_Set", new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, BonusId = BonusId, RefTransId = RefTransId }, conn, trans);
        }

        public int DoTpRewardTicket(TpCustInfo tpCustInfo, TpRewardRecordInfo tpRewardRecordInfo, TpCategory tpCategory, bool checkBonus = true)
        {
            int bonusId = 0;
            int gameType = (int)tpRewardRecordInfo.GameType;

            if (checkBonus)
            {
                GetTpBonus(tpRewardRecordInfo.TpId, tpRewardRecordInfo.TransId, gameType, tpCategory, out int custId, out bonusId);
            }

            tpRewardRecordInfo.BonusId = bonusId;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = SetTpRewardTicket(tpCustInfo, tpRewardRecordInfo, conn, trans);

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;
        }

        public int SetTpRewardTicket(TpCustInfo tpCustInfo, TpRewardRecordInfo tpRewardRecordInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            int result = 0;

            try
            {
                var RefRewardRecord = GetTpRewardRecord(tpRewardRecordInfo.TpId, tpRewardRecordInfo.TransId, (int)tpRewardRecordInfo.GameType, tpCustInfo.CustId, conn, trans);
                Enum.TryParse(tpRewardRecordInfo.TpId.ToString(), out Tp tp);

                if (tpRewardRecordInfo.Amount > 0)
                {
                    tpRewardRecordInfo.TicketStatus = TicketStatus.Win;
                }
                else if (tpRewardRecordInfo.Amount == 0)
                {
                    tpRewardRecordInfo.TicketStatus = TicketStatus.Draw;
                }
                else
                {
                    tpRewardRecordInfo.TicketStatus = TicketStatus.Lost;
                }

                if (RefRewardRecord == null)
                {
                    result = AddTpRewardRecord(tpRewardRecordInfo, tpCustInfo, conn, trans);
                }
                else
                {
                    result = SetTpRewardRecord(tpRewardRecordInfo, tpCustInfo, conn, trans);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }

            return result;
        }

        public TpRewardRecordInfo GetTpRewardRecord(int TpId, string TransId, int GameType, int CustId, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.QueryFirst<TpRewardRecordInfo>("Service_TpRewardRecord_Get", trans, new { TpId = TpId, TransId = TransId, GameType = GameType, CustId = CustId });
        }

        public int SetTpRewardRecord(TpRewardRecordInfo tpRewardRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_TpRewardRecord_Set", new
            {
                TpId = tpRewardRecordInfo.TpId,
                TransId = tpRewardRecordInfo.TransId,
                GameType = (int)tpRewardRecordInfo.GameType,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                CurrencyId = tpCustInfo.CurrencyId,
                SiteId = tpCustInfo.SiteId,
                TpUserName = tpRewardRecordInfo.TpUserName,
                SettledTime = tpRewardRecordInfo.SettledTime,
                TransTime = tpRewardRecordInfo.TransTime,
                Amount = tpRewardRecordInfo.Amount,
                SettlementType = (int)tpRewardRecordInfo.SettlementType,
                TicketStatus = (int)tpRewardRecordInfo.TicketStatus,
                BonusId = tpRewardRecordInfo.BonusId
            }, conn, trans);
        }

        public int AddTpRewardRecord(TpRewardRecordInfo tpRewardRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return _mainDbClient.Execute("Service_TpRewardRecord_Ins", new
            {
                TpId = tpRewardRecordInfo.TpId,
                TransId = tpRewardRecordInfo.TransId,
                GameType = (int)tpRewardRecordInfo.GameType,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                CurrencyId = tpCustInfo.CurrencyId,
                SiteId = tpCustInfo.SiteId,
                TpUserName = tpRewardRecordInfo.TpUserName,
                SettledTime = tpRewardRecordInfo.SettledTime,
                TransTime = tpRewardRecordInfo.TransTime,
                Amount = tpRewardRecordInfo.Amount,
                SettlementType = (int)tpRewardRecordInfo.SettlementType,
                TicketStatus = (int)tpRewardRecordInfo.TicketStatus,
                BonusId = tpRewardRecordInfo.BonusId
            }, conn, trans);
        }

        public TpBetRecordDetail GetTpBetRecordDetail(int TpId, int GameType, string TransId, DateTime SettledTime)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    TpId,
                    GameType,
                    TransId,
                    SettledTime
                });

                return _mainDbClient.QueryFirst<TpBetRecordDetail>("Service_TpBetRecordDetail_Get_V2", p);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public int SetSettleFlagOfMonthlyBettrans(string spName, BetRecordInfo betRecordInfo)
        {
            var result = 0;
            var partitionTimeOfSettledTime = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime;
            var partitionTimeOfTransTime = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.TransTime).DateTime;
            var tt = partitionTimeOfSettledTime.GetPartitionKey();
            var st = partitionTimeOfTransTime.GetPartitionKey();
            var verifyDays = (int)partitionTimeOfSettledTime.Subtract(partitionTimeOfTransTime).TotalDays;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            result = _partitionService.Execute(partitionTimeOfSettledTime, spName, new DynamicParameters(new { TransId = betRecordInfo.TransId }), trans);
            if (result > 0 && tt != st && verifyDays < 90)
            {
                // 跨月同步
                result = _partitionService.Execute(partitionTimeOfTransTime, spName, new DynamicParameters(new { TransId = betRecordInfo.TransId }), trans);
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;
        }
    }
}




