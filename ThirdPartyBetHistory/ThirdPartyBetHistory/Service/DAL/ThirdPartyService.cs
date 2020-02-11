using Dapper;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyService> logger;
        public ThirdPartyService(ILogger<ThirdPartyService> logger, IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
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
            return conn.Query<ThirdPartyModel>("Common_TpList_Get", commandType: CommandType.StoredProcedure);
        }
        public void InsertLog(string url, string input, string output)
        {
            logger.LogInformation($"Url={url}, Input={input}, Output={output}");
        }
        public void ToDoAddBetRecord(IEnumerable<BetRecordInfo> betRecordInfos)
        {
            foreach (var betRecord in betRecordInfos)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var tpCustInfo = GetTpCustInfo(betRecord.TpId, betRecord.TpUserName);
                if (tpCustInfo == null)
                {
                    continue;
                }

                if (!TpCurrencyList.ContainsKey($"{betRecord.TpId}_{tpCustInfo.CurrencyId}"))
                {
                    TpCurrencyList.TryAdd($"{betRecord.TpId}_{tpCustInfo.CurrencyId}", GetTpCurrency(betRecord.TpId, tpCustInfo.CurrencyId));
                }

                var tpCurrency = TpCurrencyList[$"{betRecord.TpId}_{tpCustInfo.CurrencyId}"];
                if (tpCurrency.ExchangeRate != 0) {
                    betRecord.Stake *= tpCurrency.ExchangeRate;
                    betRecord.WinlostAmount *= tpCurrency.ExchangeRate;
                }

                var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);

                if (RefBetRecord == null)
                {
                    if (betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, betRecord.TicketStatus == TicketStatus.Win ? betRecord.WinlostAmount : 0, "Settled", betRecord.TransId, conn, trans);
                    }
                }
                else
                {
                    if (RefBetRecord.SettlementType == SettlementType.UnSettled && betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        SetTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, betRecord.TicketStatus == TicketStatus.Win ? betRecord.WinlostAmount : 0, "Settled", betRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                    {
                        SetTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, RefBetRecord.TicketStatus == TicketStatus.Win ? RefBetRecord.WinlostAmount * -1 : 0, $"UnSettled", RefBetRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                    {
                        SetTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, RefBetRecord.TicketStatus == TicketStatus.Win ? RefBetRecord.WinlostAmount * -1 : 0, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                    }
                    else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && (RefBetRecord.Stake != betRecord.Stake || RefBetRecord.WinlostAmount != betRecord.WinlostAmount))
                    {
                        SetTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, RefBetRecord.TicketStatus == TicketStatus.Win ? RefBetRecord.WinlostAmount * -1 : 0, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                        SetTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, betRecord.TicketStatus == TicketStatus.Win ? betRecord.WinlostAmount : 0, "Re-Settled", betRecord.TransId, conn, trans);
                    }
                }

                var result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
                if (result > 0)
                {
                    trans.Commit();
                }
            }
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
        public int SetTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, decimal Winnings, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_TurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Winnings = Winnings, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetTicketKey(int TpId, long TicketKey)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_TpTicketKey_Set", new { TpId = TpId, TicketKey = TicketKey }, commandType: CommandType.StoredProcedure);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId }, commandType: CommandType.StoredProcedure);
        }
    }
}
