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
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.DAL
{
    public class ThirdPartyService
    {
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private readonly IMemoryCache cache;
        private readonly SettledService settledService;
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyService> logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly string _connectionString;

        public ThirdPartyService(IMemoryCache cache, ILogger<ThirdPartyService> logger, IWebHostEnvironment environment,
            IConfiguration configuration, SettledService settledService, ILogger<MySqlClient> mySqlLogger)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            this.cache = cache;
            this.settledService = settledService;
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
            var BetRecordInfos = betRecordInfos.OrderBy(x => x.TicketKey).ToArray();
            var idx = 0;
            BetRecordInfo betRecordInfo = null;

            while (BetRecordInfos.Length > idx)
            {
                var betRecord = Newtonsoft.Json.JsonConvert.DeserializeObject<BetRecordInfo>(Newtonsoft.Json.JsonConvert.SerializeObject(BetRecordInfos[idx]));
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

                    var Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                    var Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

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
                            //else
                            //    AutoCancelBonus(tpCustInfo.CustId, conn, trans);
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
                                SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", bonusId, betRecord.TransId, conn, trans);

                            if (Losses != 0)
                            {
                                if (hasJoinBonus)
                                    AutoCancelBonusV2(bonusId, conn, trans);
                                //else
                                //    AutoCancelBonus(tpCustInfo.CustId, conn, trans);
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
            var key = $"TpCustInfo_{tpId}_{tpUserName}";
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
            var key = $"BonusId_{tpId}_{custId}";
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
            var parameters = new DynamicParameters(new {TpId, TpUserName});
            return _reportDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", parameters);
        }
        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType)
        {
            var parameters = new DynamicParameters(new {TpId, TransId, GameType});
            return _reportDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", parameters);
        }

        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn,
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
            return _mainDbClient.Execute("Service_TpBetRecord_Set", parameters, conn, trans) ? 1 : 0;
        }

        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;
            
            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, TurnOver, Memo, RefTransId});
            return _mainDbClient.Execute("Common_RollOverTurnOver_Set", parameters, conn, trans) ? 1 : 0;
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, TurnOver, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WithdrawalLimitTurnOver_Set", parameters, conn, trans) ? 1 : 0;
        }
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, CustId, UserName, SiteId, Winnings, Losses, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WinLostAmount_Set", parameters, conn, trans) ? 1 : 0;
        }
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, MySqlConnection conn, MySqlTransaction trans)
        {
            if (validBet == 0)
                return 0;
            
            var parameters = new DynamicParameters(new {CustId, SiteId, Stake = validBet });
            return _mainDbClient.Execute("Service_EffectiveBetSum_Set", parameters, conn, trans) ? 1 : 0;
        }
        public int AutoCancelBonus(int CustId, MySqlConnection conn, MySqlTransaction trans)
        {
            var parameters = new DynamicParameters(new {CustId });
            return _mainDbClient.Execute("Service_AutoCancelBonus_Set", parameters, conn, trans) ? 1 : 0;
        }
        public int SetTicketKey(int TpId, long TicketKey)
        {
            var parameters = new DynamicParameters(new {TpId, TicketKey });
            return _mainDbClient.Execute("Service_TpTicketKey_Set", parameters) ? 1 : 0;
        }
        public int SiteTpTicketKey(int TpId, int SiteId, long TicketKey)
        {
            var parameters = new DynamicParameters(new {TpId, SiteId, TicketKey });
            return _mainDbClient.Execute("Service_SiteTpTicketKey_Set", parameters) ? 1 : 0;
        }
        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            var parameters = new DynamicParameters(new { TpId = tpId, CurrencyId = currencyId });
            return _reportDbClient.QueryFirst<TpCurrency>("TpCurrency_Get", parameters);
        }

        public int SetRollOverTurnOver(ref int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            BonusId = 0;
            if (Turnover == 0)
                return 0;

            var parameters = new DynamicParameters(new {TpId, GameType, CustId, UserName, SiteId, Turnover, Memo, RefTransId });
            parameters.Add("BonusId", BonusId, direction: ParameterDirection.InputOutput);
            var result = _mainDbClient.Execute("Service_RolloverTurnover_Set", parameters, conn, trans);
            if (result)
            {
                BonusId = parameters.Get<dynamic>("BonusId") ?? 0;
            }

            return BonusId != 0 ? 1 : 0;
        }
        public int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;
            var parameters = new DynamicParameters(new {TpId, GameType, CustId, UserName, SiteId, Winnings, Losses, Memo, BonusId, RefTransId });
            return _mainDbClient.Execute("Service_BonusWinLost_Set", parameters, conn, trans) ? 1 : 0; 
        }
        public int AutoCancelBonusV2(int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var parameters = new DynamicParameters(new { BonusId = bonusId });
            return _mainDbClient.Execute("Service_AutoCancelBonusV2_Set", parameters, conn, trans) ? 1 : 0;
        }
    }
}
