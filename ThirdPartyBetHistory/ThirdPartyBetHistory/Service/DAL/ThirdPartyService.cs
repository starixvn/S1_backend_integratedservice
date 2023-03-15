using System.Collections.Concurrent;
using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using ThirdPartyBetHistory.Service.BLL;
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
        private readonly TpBetRecordExtendBLL _tpBetRecordExtendBLL;

        public ThirdPartyService(IServiceProvider serviceProvider)
        {
            _cache = serviceProvider.GetRequiredService<IMemoryCache>();
            _logger = serviceProvider.GetRequiredService<ILogger<ThirdPartyService>>();
            _settledService = serviceProvider.GetRequiredService<SettledService>();
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _tpBetRecordExtendBLL = serviceProvider.GetRequiredService<TpBetRecordExtendBLL>();
        }

        public IEnumerable<ThirdPartyModel> GetTpList()
        {
            return _mainDbClient.Query<ThirdPartyModel>("Common_TpList_Get");
        }

        public IEnumerable<ThirdPartyModel> GetSiteTpList()
        {
            return _mainDbClient.Query<ThirdPartyModel>("Common_SiteTpList_Get");
        }

        public void DoSettleTicket(IEnumerable<SabaSettleData> settleDataList, WorkerType workerType)
        {
            foreach (var settleData in settleDataList.OrderBy(x => x.BetRecordInfo.TicketKey))
            {
                var betRecord = settleData.BetRecordInfo;
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
                    using var conn = _mainDbClient.GetMySqlConnection();
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
                    else if(RefBetRecord != null && RefBetRecord.BonusId == 0)
                    {
                        // 下注時會先寫入未結算注單進TpBetRecord，後續結算時依下注的BonusId判斷是否計算winlose、withdraw
                        hasJoinBonus = false;
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

                    #region Set WinLostAmount of Cust

                    var Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                    var Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

                    if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, trans);
                        if (hasJoinBonus)
                        {
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId,
                                tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId,
                                betRecord.TransId, trans);

                        }

                        if (bonusId > 0)
                        {
                            AutoCancelBonus(bonusId, betRecord.TransId, trans);
                            //AutoCancelSeamlessBonus(tpCustInfo, bonusId, betRecord.GameType, conn, trans);
                        }
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                                  && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        var RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                        var RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                        if (RefWinnings != Winnings || RefLosses != Losses)
                        {
                            SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, trans);
                            if (hasJoinBonus)
                            {
                                SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId,
                                    tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses,
                                    "Settled", bonusId, betRecord.TransId, trans);
                            }

                            if (bonusId > 0)
                            {
                                AutoCancelBonus(bonusId, betRecord.TransId, trans);
                                //AutoCancelSeamlessBonus(tpCustInfo, bonusId, betRecord.GameType, conn, trans);
                            }
                        }
                    }
                    
                    #endregion

                    #region Set TurnOver of RollOver OR WithdrawalLimit

                    if (hasJoinBonus) //有Bonus走SetRollOverTurnOver
                    {
                        if (RefBetRecord == null)
                        {
                            if (betRecord.SettlementType == SettlementType.SettledForValid)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int)betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet,
                                    "Settled", betRecord.TransId, trans);
                            }
                        }
                        else
                        {
                            if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int)betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet,
                                    "Settled", betRecord.TransId, trans);
                            }
                            else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int)betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    RefBetRecord.ValidBet * -1, "UnSettled", betRecord.TransId, trans);
                            }
                            else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int)betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    RefBetRecord.ValidBet * -1, "Re-Settled", RefBetRecord.TransId, trans);
                            }
                            else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && RefBetRecord.ValidBet != betRecord.ValidBet)
                            {
                                SetRollOverTurnOver(bonusId, betRecord.TpId, (int)betRecord.GameType,
                                    tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId,
                                    betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, trans);
                            }
                        }
                    }
                    else //沒Bonus走SetWithdrawalLimitTurnOver
                    {
                        if (RefBetRecord == null)
                        {
                            if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win)
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, trans);
                            }
                        }
                        else
                        {
                            if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.Draw)
                                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet, "Settled", betRecord.TransId, trans);
                            }
                            else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                        && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Draw))
                            {
                                SetWithdrawalLimitTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, trans);
                            }
                            else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost)
                                        && RefBetRecord.ValidBet != betRecord.ValidBet)
                            {
                                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, "Re-Settled", betRecord.TransId, trans);
                            }
                        }
                    }

                    #endregion

                    if ((RefBetRecord == null || RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.ValidBet, trans);
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                                  && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
                    {
                        if (RefBetRecord.ValidBet != betRecord.ValidBet)
                        {
                            SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.ValidBet - RefBetRecord.ValidBet, trans);
                        }
                    }
                    else if (RefBetRecord != null && (RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                                                  && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.Draw))
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, RefBetRecord.ValidBet * -1, trans);
                    }

                    var result = SetTpBetRecord(betRecord, tpCustInfo, trans);
                    if (result > 0)
                    {
                        trans.Commit();
                    }

                    SetSiteTicketKey(tpCustInfo.SiteId, betRecord.TpId, workerType, betRecord.TicketKey);

                    try
                    {
                        _ = Task.Run(() =>
                          {
                            _ = _tpBetRecordExtendBLL.AddTpBetRecordExtend(betRecord, tpCustInfo, settleData.TpBetRecordExtend);
                          }).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"{ex.Message} || { JsonConvert.SerializeObject(betRecord)}");
                    }
                    
                    conn.Close();
                    conn.Dispose();
                    _logger.LogDebug(JsonConvert.SerializeObject(betRecord));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, JsonConvert.SerializeObject(betRecord));
                    Thread.Sleep(1);
                }
            }
        }

        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
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
            var parameters = new DynamicParameters(new { TpId, TpUserName });
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", parameters);
        }

        private BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType)
        {
            var parameters = new DynamicParameters(new { TpId, TransId, GameType });
            return _mainDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", parameters);
        }

        private int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, IDbTransaction trans)
        {
            var parameters = new DynamicParameters(new
            {
                betRecordInfo.TpId,
                betRecordInfo.TransId,
                GameType = (int)betRecordInfo.GameType,
                tpCustInfo.CustId,
                tpCustInfo.UserName,
                tpCustInfo.CurrencyId,
                tpCustInfo.SiteId,
                betRecordInfo.TpUserName,
                betRecordInfo.TransTime,
                betRecordInfo.SettledTime,
                betRecordInfo.Stake,
                betRecordInfo.ValidBet,
                betRecordInfo.WinlostAmount,
                SettlementType = (int)betRecordInfo.SettlementType,
                TicketStatus = (int)betRecordInfo.TicketStatus,
                betRecordInfo.OriginalState,
                betRecordInfo.TicketKey,
                betRecordInfo.BonusId,
                betRecordInfo.GameID
            });
            return _mainDbClient.Execute("Service_TpBetRecord_Set_V2", parameters, trans);
        }

        private int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, IDbTransaction trans)
        {
            if (TurnOver == 0)
                return 0;

            var parameters = new DynamicParameters(new { TpId, CustId, UserName, SiteId, TurnOver, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WithdrawalLimitTurnOver_Set", parameters, trans);
        }

        private int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, IDbTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            var parameters = new DynamicParameters(new { TpId, CustId, UserName, SiteId, Winnings, Losses, Memo, RefTransId });
            return _mainDbClient.Execute("Common_WinLostAmount_Set", parameters, trans);
        }

        private int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, IDbTransaction trans)
        {
            if (validBet == 0)
                return 0;

            var parameters = new DynamicParameters(new { CustId, SiteId, Stake = validBet });
            return _mainDbClient.Execute("Service_EffectiveBetSum_Set", parameters, trans);
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            var parameters = new DynamicParameters(new { TpId = tpId, CurrencyId = currencyId });
            return _mainDbClient.QueryFirst<TpCurrency>("TpCurrency_Get", parameters);
        }

        private int SetRollOverTurnOver(int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, IDbTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var parameters = new DynamicParameters(new { TpId, GameType, CustId, UserName, SiteId, Turnover, Memo, RefTransId });
            parameters.Add("BonusId", BonusId, direction: ParameterDirection.InputOutput);
            var result = _mainDbClient.Execute("Service_RolloverTurnover_Set_V2", parameters, trans);
            BonusId = parameters.Get<int>("BonusId");
            return BonusId > 0 ? 1 : 0;
        }

        private int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, IDbTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;
            var parameters = new DynamicParameters(new { TpId, GameType, CustId, UserName, SiteId, Winnings, Losses, Memo, BonusId, RefTransId });
            return _mainDbClient.Execute("Service_BonusWinLost_Set", parameters, trans);
        }

        private int AutoCancelBonus(int bonusId, string transId, IDbTransaction trans)
        {
            var parameters = new DynamicParameters(new { BonusId = bonusId, TransId = transId });
            return _mainDbClient.Execute("Service_AutoCancelBonus_Set_V3", parameters,  trans);
        }

        private int AutoCancelSeamlessBonus(TpCustInfo tpCustInfo, int bonusId, GameType gameType, IDbTransaction trans)
        {
            var p = new DynamicParameters(tpCustInfo);
            p.AddDynamicParams(new { BonusId = bonusId, GameType = (int)gameType });
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("Service_AutoCancelSeamlessBonusV2_Set", p, trans);
            var bonus = p.Get<decimal?>("Bonus") ?? 0;

            if (bonus > 0)
            {
                result = RefundCancelBonus(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bonus, $"Bonus: {Math.Abs(bonus).ToString("#,##0.##")}", bonusId.ToString(), out _, trans);
            }

            return result;
        }
        private int RefundCancelBonus(int custId, int siteId, string userName, decimal Amount, string Memo, string RefTransId, out decimal afterAmount, IDbTransaction trans)
        {
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount, Type = 5, Memo = Memo, RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            _ = _mainDbClient.Execute("Wallet_Set_V2", p, trans);
            _ = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal>("AfterAmount");
            return afterAmount >= 0 ? 1 : 0;
        }


        public IEnumerable<UnSettle> GetTpBetRecordReSettleTicket(Tp tp, SiteId siteId, string settlementTypeList,
            DateTime startTime, DateTime endTime)
        {
            return _mainDbClient.Query<UnSettle>("Service_TpBetRecord_ReSettle_Get",
                new
                {
                    _TpId = (int)tp, _SiteId = (int)siteId, SettlementTypeList = settlementTypeList,
                    StartTime = startTime, EndTime = endTime
                });
        }

        public long GetSiteTicketKey(SiteId SiteId, int TpId, WorkerType workerType)
        {
            var parameters = new { SiteId = (int)SiteId, TpId, WorkerType = (int)workerType };
            return _mainDbClient.QueryFirst<long>("Service_SiteTicketKey_Get", parameters);
        }
        
        public int SetSiteTicketKey(int SiteId, int TpId, WorkerType workerType, long TicketKey)
        {
            var parameters = new { SiteId, TpId, WorkerType = (int)workerType, TicketKey };
            return _mainDbClient.Execute("Service_SiteTicketKey_Set", parameters);
        }

        public int AddTpBetRecordDetail(TpBetRecordDetail detail)
        {
            return _mainDbClient.QueryFirst<int>("Service_TpBetRecordDetail_Ins_V2", detail); 
        }
    }
}
