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
            var result = 0;
            try
            {
                var tpBonus = GetTpBonus(tpId, tpTransId, gameType, tpCategory, out var custId, out bonusId);

                if (bonusId > 0 || transType == TransType.Prize)
                {
                    if (bonusId > 0 && custId != tpCustInfo.CustId)
                    {
                        return 0;
                    }
                    
                    var setBonusAmount = amount;
                    var diffAmount = 0m; 
                    // Refund需依照各自錢包實際使用的金額，退回各自錢包
                    if (transType ==  TransType.Refunds)
                    {
                        diffAmount = amount - tpBonus.UseBonus; //派彩金額與實際使用bonus的差額
                        setBonusAmount = diffAmount > 0  ? tpBonus.UseBonus : amount;
                    }

                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, setBonusAmount, tpId, transType, $"{memoHead}: {Math.Abs(amount):#,##0.##}", refId, out var afterBonus, ref bonusId, conn, trans);

                    if ((transType == TransType.Prize && bonusId == 0) || afterBonus < 0 || bonusId == -1 || diffAmount > 0)
                    {
                        var setBalanceAmount = diffAmount > 0 ? diffAmount : afterBonus;
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, setBalanceAmount, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus):#,##0.##}", refId, out afterAmount, conn, trans, allowNegative);
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

        public UseTpSeamlessBonus GetTpBonus(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId, GetTpSeamlessBonusType getTpSeamlessBonusType = GetTpSeamlessBonusType.ByGameTypeAndTransId)
        {
            return GetUseTpSeamlessBonus(tpId, tpTransId, gameType, out CustId, out BonusId, getTpSeamlessBonusType);
        }

        private UseTpSeamlessBonus GetUseTpSeamlessBonus(int TpId, string TransId, int GameType, out int CustId, out int BonusId, GetTpSeamlessBonusType getTpSeamlessBonusType)
        {
            CustId = 0;
            BonusId = 0;
            var spName = getTpSeamlessBonusType == GetTpSeamlessBonusType.ByGameTypeAndTransId ?
                "Service_TpSeamlessBonus_Get_V2" : "Service_TpSeamlessBonus_GetByTransID_V2";
            var p = new DynamicParameters(new { TpId, TransId, GameType });

            var useTpSeamlessBonus =  _mainDbClient.QueryFirst<UseTpSeamlessBonus>(spName, parameters: p);
            if (useTpSeamlessBonus != null)
            {
                CustId = useTpSeamlessBonus.CustId;
                BonusId = useTpSeamlessBonus.BonusId;
            }

            return useTpSeamlessBonus;
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
            GetUseTpSeamlessBonus(betRecordInfo.TpId, betRecordInfo.TransId, gameType, out int custId, out bonusId, getTpSeamlessBonusType);
            if (bonusId == 0 && !string.IsNullOrEmpty(betRecordInfo.ReferenceNo))
            {
                GetUseTpSeamlessBonus(betRecordInfo.TpId, betRecordInfo.ReferenceNo, gameType, out custId, out bonusId, getTpSeamlessBonusType);
            }

            betRecordInfo.BonusId = bonusId;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecordInfo, trans, getTpSeamlessBonusType);

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

        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, MySqlTransaction trans, GetTpSeamlessBonusType getTpSeamlessBonusType = GetTpSeamlessBonusType.ByGameTypeAndTransId)
        {
            int result;
            try
            {
                // 取得前一次的結算結果
                var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, trans);

                _ = new SettleTicket(
                        tpCustInfo: tpCustInfo,
                        betRecord: betRecord,
                        refBetRecord: RefBetRecord,
                        mainDbClient: _mainDbClient,
                        trans: trans,
                        walletService: walletService,
                        logger: logger,
                        tpBetRecordExtendBLL: _tpBetRecordExtendBLL)
                    .SetData()
                    .CheckRebateExcludeGame()
                    .SetTpSeamlessBonusFlag()
                    .SetWinLost()
                    .AutoCancelCheck()
                    .SetBonusRollOver()
                    .SetWithdrawalLimitAndEffectiveBet(getTpSeamlessBonusType)
                    .SetTpBetRecord()
                    .AddTpBetRecordExtend();

                result = 1;
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

        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlTransaction trans)
        {
            return _mainDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", trans, new { TpId, TransId, GameType });
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




