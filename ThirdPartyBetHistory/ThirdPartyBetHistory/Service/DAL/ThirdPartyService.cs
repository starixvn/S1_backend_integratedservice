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
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyService> logger;
        public ThirdPartyService(IMemoryCache cache, ILogger<ThirdPartyService> logger, IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            this.cache = cache;
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
            foreach (var betRecord in betRecordInfos)
            {
                int result = 0;
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var tpCustInfo = GetTpCustInfoWithCache(betRecord.TpId, betRecord.TpUserName);
                if (tpCustInfo == null/* || betRecord.TicketStatus == TicketStatus.UnSettled*/)
                {
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
                    betRecord.WinlostAmount *= tpCurrency.ExchangeRate;
                }

                var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);

                if (RefBetRecord == null)
                {
                    if (betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                    }
                }
                else
                {
                    if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                    {
                        SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, $"UnSettled", RefBetRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                    {
                        SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && RefBetRecord.Stake != betRecord.Stake)
                    {
                        SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, "Re-Settled", betRecord.TransId, conn, trans);
                    }
                }

                if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                {
                    SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                }
                else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                    && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled))
                {
                    SetWithdrawalLimitTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
                }
                else if (RefBetRecord != null
                    && (betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost)
                    && RefBetRecord.Stake != betRecord.Stake)
                {
                    SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, "Re-Settled", betRecord.TransId, conn, trans);
                }

                decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

                if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                    AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                }
                else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                {
                    decimal RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                    decimal RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                    if (RefWinnings != Winnings || RefLosses != Losses)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, conn, trans);
                        AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                    }
                }

                if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake, conn, trans);
                }
                else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                {
                    if (RefBetRecord.Stake != betRecord.Stake)
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, conn, trans);
                    }
                }

                result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
                if (result > 0)
                {
                    trans.Commit();
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
            return conn.Execute("Service_TpBetRecord_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, TransTime = betRecordInfo.TransTime, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus, OriginalState = betRecordInfo.OriginalState, TicketKey = betRecordInfo.TicketKey }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_RollOverTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_WinLostAmount_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal Stake, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_EffectiveBetSum_Set", new { CustId = CustId, SiteId = SiteId, Stake = Stake }, transaction: trans, commandType: CommandType.StoredProcedure);
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
    }
}
