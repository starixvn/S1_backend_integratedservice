using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class ThirdPartyService
    {
        private static ConcurrentDictionary<int, IThirdPartyBetHelper> ThirdPartyHelperContainer = new ConcurrentDictionary<int, IThirdPartyBetHelper>();
        private static ConcurrentDictionary<int, ThirdPartyModel> ThirdPartyModelContainer = new ConcurrentDictionary<int, ThirdPartyModel>();
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private readonly IMemoryCache cache;
        private readonly SettledService settledService;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyService> logger;
        public ThirdPartyService(IMemoryCache cache, ILogger<ThirdPartyService> logger, IWebHostEnvironment environment, IConfiguration configuration, SettledService settledService)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            this.cache = cache;
            this.settledService = settledService;
        }
        public ThirdPartyModel GetThirdPartyModel(int tpId)
        {
            if (!ThirdPartyModelContainer.ContainsKey(tpId))
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                var thirdPartyModel = conn.QueryFirstOrDefault<ThirdPartyModel>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(tpId, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(tpId) ? ThirdPartyModelContainer[tpId] : null;
        }

        public IEnumerable<ThirdPartyModel> GetTpList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var tpList = conn.Query<ThirdPartyModel>("Common_TpList_Get", commandType: CommandType.StoredProcedure);

            return tpList;
        }

        public IEnumerable<ThirdPartyModel> GetSiteTpList()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var siteTpList = conn.Query<ThirdPartyModel>("Common_SiteTpList_Get", commandType: CommandType.StoredProcedure);

            return siteTpList;
        }
        public void InsertLog(string url, string input, string output)
        {
            logger.LogInformation($"Url={url}, Input={input}, Output={output}");
        }
        public void ToDoAddBetRecord(IEnumerable<BetRecordInfo> betRecordInfos)
        {
            var BetRecordInfos = betRecordInfos.OrderBy(x => x.TicketKey).ToArray();
            int idx = 0;
            BetRecordInfo betRecordInfo = null;
            while (BetRecordInfos.Length > idx)
            {
                int result = 0;
                var betRecord = BetRecordInfos[idx];
                betRecordInfo = betRecord;
                var tpCustInfo = GetTpCustInfoWithCache(betRecord.TpId, betRecord.TpUserName);

                if (tpCustInfo == null/* || betRecord.TicketStatus == TicketStatus.UnSettled*/)
                {
                    idx++;
                    continue;
                }

                if (!TpCurrencyList.ContainsKey($"{betRecord.TpId}_{tpCustInfo.CurrencyId}"))
                {
                    TpCurrencyList.TryAdd($"{betRecord.TpId}_{tpCustInfo.CurrencyId}", GetTpCurrency(betRecord.TpId, tpCustInfo.CurrencyId));
                }

                var tpCurrency = TpCurrencyList[$"{betRecord.TpId}_{tpCustInfo.CurrencyId}"];
                if (tpCurrency.ExchangeRate != 0)
                {
                    betRecord.Stake *= tpCurrency.ExchangeRate;
                    betRecord.ValidBet *= tpCurrency.ExchangeRate;
                    betRecord.WinlostAmount *= tpCurrency.ExchangeRate;
                }

                try
                {
                    using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                    conn.Open();
                    using var trans = conn.BeginTransaction();
                    bool hasJoinBonus = false;
                    int bonusId = 0;

                    /*Only count Sports*/
                    if (betRecord.GameType != GameType.Saba_Sports && betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        betRecord.SettlementType = SettlementType.SettledForValidNotRollover;
                    }

                    var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);

                    if (RefBetRecord != null && RefBetRecord.BonusId > 0)
                    {
                        hasJoinBonus = true;
                        betRecord.BonusId = RefBetRecord.BonusId;
                        bonusId = betRecord.BonusId;
                    }
                    else
                    {
                        hasJoinBonus = settledService.GetBonusIdByTpId(1, tpCustInfo.CustId, out bonusId);
                        if (hasJoinBonus && (RefBetRecord == null || betRecord.TicketStatus == TicketStatus.UnSettled))
                        {
                            betRecord.BonusId = bonusId;
                        }
                    }

                    var isRollover = false;
                    var hasBonusRunning = false;
                    if (RefBetRecord == null)
                    {
                        if (betRecord.SettlementType == SettlementType.SettledForValid)
                        {
                            if (hasJoinBonus)
                            {
                                hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans) > 0;
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans) > 0;
                            }
                        }
                    }
                    else
                    {
                        if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                        {
                            if (hasJoinBonus)
                            {
                                hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans) > 0;
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans) > 0;
                            }
                        }
                        else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                        {
                            if (hasJoinBonus)
                            {
                                hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, $"UnSettled", betRecord.TransId, conn, trans) > 0;
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, $"UnSettled", RefBetRecord.TransId, conn, trans) > 0;
                            }
                        }
                        else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                        {
                            if (hasJoinBonus)
                            {
                                hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans) > 0;
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans) > 0;
                            }
                        }
                        else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && RefBetRecord.ValidBet != betRecord.ValidBet)
                        {
                            if (hasJoinBonus)
                            {
                                hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, conn, trans) > 0;
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, conn, trans) > 0;
                            }
                        }
                    }

                    if (!isRollover && !hasJoinBonus)
                    {
                        if (RefBetRecord == null)
                        {
                            if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win)
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans);
                            }
                        }
                        else
                        {
                            if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.Draw)
                            && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, conn, trans);
                            }
                            else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Draw))
                            {
                                SetWithdrawalLimitTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
                            }
                            else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost)
                                && RefBetRecord.ValidBet != betRecord.ValidBet)
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                            }
                        }
                    }

                    decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                    decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

                    if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                        if (hasJoinBonus)
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                        if (Losses != 0)
                        {
                            if (hasJoinBonus)
                                AutoCancelBonusV2(bonusId, conn, trans);
                            else
                                AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                        }
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        decimal RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                        decimal RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                        if (RefWinnings != Winnings || RefLosses != Losses)
                        {
                            SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, conn, trans);
                            if (hasJoinBonus)
                                SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", bonusId, betRecord.TransId, conn, trans);

                            if (Losses != 0)
                            {
                                if (hasJoinBonus)
                                    AutoCancelBonusV2(bonusId, conn, trans);
                                else
                                    AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                            }
                        }
                    }

                    if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.ValidBet, conn, trans);
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        if (RefBetRecord.ValidBet != betRecord.ValidBet)
                        {
                            SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, conn, trans);
                        }
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                        && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.Draw))
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, conn, trans);
                    }

                    result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }

                    if (tpCustInfo.SiteId == 1)
                    {
                        SetTicketKey(1, betRecord.TicketKey);
                    }
                    else
                    {
                        SiteTpTicketKey(1, tpCustInfo.SiteId, betRecord.TicketKey);
                    }

                    logger.LogInformation($"{Newtonsoft.Json.JsonConvert.SerializeObject(betRecord)}");

                    idx++;
                }
                catch (Exception ex)
                {
                    logger.LogError($"{Newtonsoft.Json.JsonConvert.SerializeObject(betRecordInfo)}\r\n{ex}");
                    System.Threading.Thread.Sleep(1);
                }
            }
        }
        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            string key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return tpCustInfo;
        }
        public bool GetBonusIdByTpId(int tpId, int custId, out int bonusId)
        {
            string key = $"BonusId_{tpId}_{custId}";
            bonusId = cache.Get<int?>(key) ?? 0;
            if (bonusId == 0)
            {
                if (settledService.GetBonusIdByTpId(tpId, custId, out bonusId))
                {
                    cache.Set(key, bonusId, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(3)));
                    return true;
                }
            }
            else
            {
                return true;
            }

            return false;
        }
        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName }, commandType: CommandType.StoredProcedure);
        }
        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.QueryFirstOrDefault<BetRecordInfo>("Service_TpBetRecord_Get", new { TpId = TpId, TransId = TransId, GameType = GameType }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpBetRecord_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, TransTime = betRecordInfo.TransTime, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, betRecordInfo.ValidBet, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus, OriginalState = betRecordInfo.OriginalState, TicketKey = betRecordInfo.TicketKey, BonusId = betRecordInfo.BonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            return conn.Execute("Common_RollOverTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            return conn.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        public int SetTicketKey(int TpId, long TicketKey)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_TpTicketKey_Set", new { TpId = TpId, TicketKey = TicketKey }, commandType: CommandType.StoredProcedure);
        }
        public int SiteTpTicketKey(int TpId, int SiteId, long TicketKey)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_SiteTpTicketKey_Set", new { TpId = TpId, SiteId = SiteId, TicketKey = TicketKey }, commandType: CommandType.StoredProcedure);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }

        public int SetRollOverTurnOver(ref int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var p = new DynamicParameters(new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Turnover = Turnover, Memo, RefTransId });
            p.Add("BonusId", value: BonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            var result = conn.Execute("Service_RolloverTurnover_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            BonusId = p.Get<int>("BonusId");
            return result;
        }
        public int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            return conn.Execute("Service_BonusWinLost_Set", new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, BonusId = BonusId, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelBonusV2(int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelBonusV2_Set", new { BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
    }
}
