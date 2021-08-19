using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class ThirdPartyService
    {
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private readonly IMemoryCache _cache;
        private readonly SettledService _settledService;
        private readonly ILogger<ThirdPartyService> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _connectionString;

        public ThirdPartyService(IServiceProvider serviceProvider)
        {
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();
            _logger = serviceProvider.GetRequiredService<ILogger<ThirdPartyService>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            _settledService = serviceProvider.GetRequiredService<SettledService>();
            var mySqlLogger = serviceProvider.GetRequiredService<ILogger<MySqlClient>>();
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            _mainDbClient = new MySqlClient(_connectionString, mySqlLogger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), mySqlLogger);
        }

        public IEnumerable<ThirdPartyModel> GetTpList()
        {
            return _reportDbClient.Query<ThirdPartyModel>("Common_TpList_Get");
        }

        public IEnumerable<ThirdPartyModel> GetSiteTpList()
        {
            return _reportDbClient.Query<ThirdPartyModel>("Common_SiteTpList_Get");
        }

        public void ToDoAddBetRecord(IEnumerable<BetRecordInfo> betRecordInfos)
        {
            foreach (var betRecord in betRecordInfos.OrderBy(x => x.TicketKey))
            {
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
                if (tpCurrency.ExchangeRate > 1)
                {
                    betRecord.Stake *= tpCurrency.ExchangeRate;
                    betRecord.ValidBet *= tpCurrency.ExchangeRate;
                    betRecord.WinlostAmount *= tpCurrency.ExchangeRate;
                }

                try
                {
                    using var conn = new MySqlConnection(_connectionString);
                    conn.Open();
                    using var trans = conn.BeginTransaction();
                    bool hasJoinBonus;
                    int bonusId;

                    /*Only count Sports*/
                    if (betRecord.GameType != GameType.Saba_Sports && betRecord.SettlementType == SettlementType.SettledForValid)
                    {
                        betRecord.SettlementType = SettlementType.SettledForValidNotRollover;
                    }

                    var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType);

                    if (RefBetRecord != null && RefBetRecord.BonusId > 0)
                    {
                        hasJoinBonus = true;
                        betRecord.BonusId = RefBetRecord.BonusId;
                        bonusId = betRecord.BonusId;
                    }
                    else
                    {
                        hasJoinBonus = _settledService.GetBonusIdByTpId((int)Tp.Saba, tpCustInfo.CustId, out bonusId);
                        if (hasJoinBonus && (RefBetRecord == null || betRecord.TicketStatus == TicketStatus.UnSettled))
                        {
                            betRecord.BonusId = bonusId;
                        }
                    }

                    var isRollover = false;
                    if (RefBetRecord == null)
                    {
                        if (betRecord.SettlementType == SettlementType.SettledForValid)
                        {
                            if (hasJoinBonus)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int) betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet,
                                    "Settled", betRecord.TransId, conn, trans);
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
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int) betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet,
                                    "Settled", betRecord.TransId, conn, trans);
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
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int) betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    RefBetRecord.ValidBet * -1, "UnSettled", betRecord.TransId, conn, trans);
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, "UnSettled", RefBetRecord.TransId, conn, trans) > 0;
                            }
                        }
                        else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                        {
                            if (hasJoinBonus)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int) betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    RefBetRecord.ValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans);
                            }
                            else
                            {
                                isRollover = SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans) > 0;
                            }
                        }
                        else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && RefBetRecord.ValidBet != betRecord.ValidBet)
                        {
                            if (hasJoinBonus)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int) betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, conn,
                                    trans);
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

                    var Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                    var Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

                    if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                        if (hasJoinBonus)
                            SetBonusWinLost(betRecord.TpId, (int) betRecord.GameType, tpCustInfo.CustId,
                                tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId,
                                betRecord.TransId, conn, trans);

                        if (Losses != 0 && bonusId > 0)
                        {
                            AutoCancelBonusV2(bonusId, conn, trans);
                        }
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                                  && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        var RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                        var RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                        if (RefWinnings != Winnings || RefLosses != Losses)
                        {
                            SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, conn, trans);
                            if (hasJoinBonus)
                                SetBonusWinLost(betRecord.TpId, (int) betRecord.GameType, tpCustInfo.CustId,
                                    tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses,
                                    "Settled", bonusId, betRecord.TransId, conn, trans);

                            if (Losses != 0 && bonusId > 0)
                            {
                                AutoCancelBonusV2(bonusId, conn, trans);
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

                    var result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }

                    if (tpCustInfo.SiteId == (int)SiteId.Nova88)
                    {
                        SetTicketKey((int)Tp.Saba, betRecord.TicketKey);
                    }
                    else
                    {
                        SiteTpTicketKey((int)Tp.Saba, tpCustInfo.SiteId, betRecord.TicketKey);
                    }

                    _logger.LogInformation(JsonConvert.SerializeObject(betRecord));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, JsonConvert.SerializeObject(betRecord));
                    Thread.Sleep(1);
                }
            }
        }

        private TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            var key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = _cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    _cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5)));
                }
            }

            return tpCustInfo;
        }

        private TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            var parameters = new DynamicParameters(new {TpId, TpUserName});
            return _reportDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", parameters);
        }

        private BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType)
        {
            var parameters = new DynamicParameters(new {TpId, TransId, GameType});
            return _reportDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", parameters);
        }

        private int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn,
            MySqlTransaction trans)
        {
            var parameters = new DynamicParameters(new
            {
                betRecordInfo.TpId, betRecordInfo.TransId, GameType = (int) betRecordInfo.GameType, tpCustInfo.CustId,
                tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, betRecordInfo.TpUserName,
                betRecordInfo.TransTime, betRecordInfo.SettledTime, betRecordInfo.Stake, betRecordInfo.ValidBet,
                betRecordInfo.WinlostAmount, SettlementType = (int) betRecordInfo.SettlementType,
                TicketStatus = (int) betRecordInfo.TicketStatus, betRecordInfo.OriginalState, betRecordInfo.TicketKey,
                betRecordInfo.BonusId
            });
            return _mainDbClient.Execute("Service_TpBetRecord_Set", parameters, conn, trans);
        }

        private int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, TurnOver, Memo, RefTransId});
            return _mainDbClient.Execute("Common_RollOverTurnOver_Set", parameters, conn, trans);
        }

        private int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, TurnOver, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WithdrawalLimitTurnOver_Set", parameters, conn, trans);
        }

        private int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, Winnings, Losses, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WinLostAmount_Set", parameters, conn, trans);
        }

        private int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, MySqlConnection conn, MySqlTransaction trans)
        {
            if (validBet == 0)
                return 0;
            
            var parameters = new DynamicParameters(new {CustId, SiteId, Stake = validBet });
            return _mainDbClient.Execute("Service_EffectiveBetSum_Set", parameters, conn, trans);
        }

        public int SetTicketKey(int TpId, long TicketKey)
        {
            var parameters = new DynamicParameters(new {TpId, TicketKey });
            return _mainDbClient.Execute("Service_TpTicketKey_Set", parameters);
        }
        public int SiteTpTicketKey(int TpId, int SiteId, long TicketKey)
        {
            var parameters = new DynamicParameters(new {TpId, SiteId, TicketKey });
            return _mainDbClient.Execute("Service_SiteTpTicketKey_Set", parameters);
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            var parameters = new DynamicParameters(new { TpId = tpId, CurrencyId = currencyId });
            return _reportDbClient.QueryFirst<TpCurrency>("TpCurrency_Get", parameters);
        }

        private int SetRollOverTurnOver(int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, GameType, CustId, UserName, SiteId, Turnover, Memo, RefTransId });
            parameters.Add("BonusId", BonusId, direction: ParameterDirection.InputOutput);
            return _mainDbClient.Execute("Service_RolloverTurnover_Set", parameters, conn, trans);
        }

        private int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;
            var parameters = new DynamicParameters(new {TpId, GameType, CustId, UserName, SiteId, Winnings, Losses, Memo, BonusId, RefTransId });
            return _mainDbClient.Execute("Service_BonusWinLost_Set", parameters, conn, trans);
        }

        private int AutoCancelBonusV2(int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var parameters = new DynamicParameters(new { BonusId = bonusId });
            return _mainDbClient.Execute("Service_AutoCancelBonusV2_Set", parameters, conn, trans);
        }
    }
}
