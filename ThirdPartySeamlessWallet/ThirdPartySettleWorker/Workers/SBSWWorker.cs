using System.ComponentModel;
using System.Text;
using Dapper;
using Newtonsoft.Json;
using Serilog.Context;
using StackExchange.Redis;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.Options;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;
using System.Diagnostics;

namespace ThirdPartySettleWorker.Workers
{
    public class SBSWWorker : BackgroundService, ISettleWorker
    {
        public Tp Tp { get => Tp.SBSW; }
        private Timer timer;
        private readonly string _tpName;
        private readonly TpUnSettledBLL _tpUnSettledBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Dictionary<int, List<SabaSportTypeOption>> _tpGameTypeConfig;
        private readonly ILogger<SBSWWorker> _logger;

        public SBSWWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _logger = provider.GetRequiredService<ILogger<SBSWWorker>>();
            _tpGameTypeConfig = provider.GetRequiredService<IEnumerable<SabaSportTypeOption>>()
                .GroupBy(c => c.TpGameType).ToDictionary(c => c.Key, c => c.ToList());
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(SettleSBSW, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }

        private void SettleSBSW(object state)
        {
            LogContext.PushProperty("TP", _tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            var dueTimeSpan = TimeSpan.FromSeconds(3);

            try
            {
                var lgsw1 = Stopwatch.StartNew();
                lgsw1.Start();
                var tpUnsettleList = _settledBLL.GetSBSWUnsettleList();
                lgsw1.Stop();
                _logger.LogWarning($"GetSBSWUnsettleList Count: {tpUnsettleList.Count()} ({lgsw1.Elapsed})");

                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }

                if (tpUnsettleList.Count() >= 1000)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                foreach (var tpUnsettle in tpUnsettleList.OrderBy(x => x.TransId))
                {
                    LogContext.PushProperty("TransId", tpUnsettle.TransId);
                    var settleFlowLog = new StringBuilder();
                    settleFlowLog.AppendLine($"TransId: {tpUnsettle.TransId} - Start Settle Flow");
                    settleFlowLog.AppendLine($"===================================================");

                    try
                    {
                        // 1.取得Wager資料
                        var lgsw2 = Stopwatch.StartNew();
                        lgsw2.Start();
                        var tpBetRecordDetail = _settledBLL.GetTpBetRecordDetail((int)Tp.SBSW, (int)tpUnsettle.GameType, tpUnsettle.TransId, tpUnsettle.PartitionTime);
                        var wagerData = GetWagerData(tpBetRecordDetail);
                        lgsw2.Stop();
                        settleFlowLog.AppendLine($"1.Get Wager Data: {lgsw2.Elapsed}");
                        settleFlowLog.AppendLine(JsonConvert.SerializeObject(wagerData));
                        settleFlowLog.AppendLine($"===================================================");

                        // 2.取得Settle資料
                        var lgsw3 = Stopwatch.StartNew();
                        lgsw3.Start();
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleSBSW>("Service_Bettrans_SBSW_Settle_Get", new { txId = tpUnsettle.TransId }, DataBase.Main);
                        lgsw3.Stop();
                        settleFlowLog.AppendLine($"2.Get Settle Data: {lgsw3.Elapsed}");
                        settleFlowLog.AppendLine(JsonConvert.SerializeObject(settleData));
                        settleFlowLog.AppendLine($"===================================================");

                        // 3.取得Bet資料
                        var getBettransSp = "Service_Bettrans_SBSW_Bet_Get";
                        var parameters = new DynamicParameters(new { txId = tpUnsettle.TransId });

                        // Vgaming有Settle資料沒有被Cancel，需要改用refId去取得Bet Data
                        if (tpUnsettle.GameType == GameType.SBSW_Vgaming && settleData.Any() && !isCancelStatus(wagerData))
                        {
                            getBettransSp = "Service_Bettrans_SBSW_Bet_refId_Get";
                            parameters = new DynamicParameters(new { settleData.FirstOrDefault()?.refId });
                        }

                        var lgsw4 = Stopwatch.StartNew();
                        lgsw4.Start();
                        var betData = _tpUnSettledBLL.GetTpUnsettleData<BetSBSW>(getBettransSp, parameters, DataBase.Main);
                        lgsw4.Stop();
                        settleFlowLog.AppendLine($"3.Get Bet Data: {lgsw4.Elapsed}");
                        settleFlowLog.AppendLine(JsonConvert.SerializeObject(betData));
                        settleFlowLog.AppendLine($"getBettransSp: {getBettransSp}");
                        settleFlowLog.AppendLine($"===================================================");

                        // 取不到下注資料，直接跳過這筆
                        if (!betData.Any())
                        {
                            settleFlowLog.AppendLine($"{tpUnsettle.TransId} Bet Data is empty, continue next TransId.");
                            continue;
                        }

                        // 設定sport_type，Vgaming收到的sport_type是空的，另外進行設定
                        var sport_type = betData.FirstOrDefault().sportType;
                        if (tpUnsettle.GameType == GameType.SBSW_Vgaming)
                        {
                            sport_type = 171;
                        }

                        // 取得相關設定資料
                        var lgsw5 = Stopwatch.StartNew();
                        lgsw5.Start();
                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, wagerData.vendor_member_id);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);
                        lgsw5.Stop();
                        settleFlowLog.AppendLine($"3A.GetTpCustInfoWithCache & GetTpCurrency: {lgsw5.Elapsed}");
                        settleFlowLog.AppendLine($"===================================================");
                        if (!_tpGameTypeConfig.TryGetValue((int)tpUnsettle.GameType, out var tpGameTypeData))
                        {
                            throw new SBSWException(ReturnCode.InvalidParameters, $"GameType:{(int)tpUnsettle.GameType} not supported.");
                        }

                        // 匯率轉換
                        var lgsw6 = Stopwatch.StartNew();
                        lgsw6.Start();
                        wagerData.stake *= tpCurrency.ExchangeRate;
                        wagerData.winlost_amount *= tpCurrency.ExchangeRate;

                        // 未結算預設值
                        var winlostAmount = 0m;
                        var settlementType = SettlementType.UnSettled;
                        var ticketStatus = TicketStatus.UnSettled;
                        var transTime = wagerData.transaction_time;
                        var settledTime = string.IsNullOrWhiteSpace(wagerData.settlement_time) ? wagerData.transaction_time : DateTime.Parse(wagerData.settlement_time);
                        var tpCategoryId = tpGameTypeData.FirstOrDefault().TpCategoryId;

                        // 4.設定預設BetRecordInfo
                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = wagerData.stake,
                            ValidBet = wagerData.stake,
                            WinlostAmount = winlostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = tpUnsettle.GameType,
                            TpUserName = wagerData.vendor_member_id,
                            SettledTime = new DateTimeOffset(settledTime.Ticks, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds(),
                            TransTime = new DateTimeOffset(transTime.Ticks, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds(),
                            SettlementType = settlementType,
                            TicketStatus = ticketStatus,
                            GameID = sport_type.ToString(),
                            ReferenceNo = betData.FirstOrDefault()?.refId,
                            TicketKey = betData.FirstOrDefault()?.baStatus ?? 0
                        };

                        lgsw6.Stop();
                        settleFlowLog.AppendLine($"4.Set Default BetRecordInfo: {lgsw6.Elapsed}");
                        settleFlowLog.AppendLine(JsonConvert.SerializeObject(betRecordInfo));
                        settleFlowLog.AppendLine($"===================================================");

                        // 5.處理沒有收到SettleData的狀況
                        if (!settleData.Any())
                        {
                            settleFlowLog.AppendLine($"{tpUnsettle.TransId} Settle Data is empty,");

                            // 下注單有被Cancel
                            if (betData.Any(x => x.TicketType == (int)SBSWTransType.CancelBet))
                            {
                                var lgsw7 = Stopwatch.StartNew();
                                lgsw7.Start();

                                // 走共用流判斷ValidBet、SettlementType、TicketStatus
                                CalculateAvailableBet(wagerData, betRecordInfo);
                                settleFlowLog.AppendLine($"{tpUnsettle.TransId} is cancel bet.");

                                lgsw7.Stop();
                                settleFlowLog.AppendLine($"4A.CancelBet : {lgsw7.Elapsed}");
                                settleFlowLog.AppendLine($"===================================================");
                            }
                            else
                            {
                                // Wager的注單狀態為reject或refund，判斷當初可能下注或是Cancel沒有收到
                                // 先不做Settle，把該筆從TpUnsettle移除
                                if (isCancelStatus(wagerData))
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, true);
                                    settleFlowLog.AppendLine($"{tpUnsettle.TransId} is rejected.");
                                    continue;
                                }

                                var lgsw10 = Stopwatch.StartNew();
                                lgsw10.Start();

                                // 拿TpBetRecord判斷，判斷要執行Settle流程，還是等待API收到Settle後才執行
                                var tpBetRecord = _settledBLL.GetSBSWTpBetRecord((int)Tp.SBSW, tpUnsettle.TransId, (int)tpUnsettle.GameType);

                                lgsw10.Stop();
                                settleFlowLog.AppendLine($"4B.GetSBSWTpBetRecord : {lgsw10.Elapsed}");

                                // Wager的注單狀態已經更新，但我方API還沒收到做Settle
                                if (tpBetRecord != null)
                                {
                                    // 先將此筆留在TpUnsettle，透過Hangfire排程每10分鐘重置結算狀態
                                    // 等下一次重置後，再次判斷API是否有收到Settle
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, false);
                                    settleFlowLog.AppendLine($"{tpUnsettle.TransId} waiting Settle Data.");

                                    continue;
                                }
                                else
                                {
                                    // Settle還沒收到資料，一樣正常執行Settle流程，因為有以下兩種情境
                                    // 情境1：體育的注單執行UnSettled流程
                                    // 情境2：主播打賞可能會立刻拿到Wager更新，但還沒收到Settle
                                    settleFlowLog.AppendLine($"{tpUnsettle.TransId} Execute UnSettled or Tip.");
                                }
                            }
                        }

                        // 6.下注金額與Wager比對結算金額
                        if (betData.Sum(x => x.betAmount) == wagerData.stake || tpUnsettle.GameType == GameType.SBSW_Vgaming)
                        {
                            var lgsw8 = Stopwatch.StartNew();
                            lgsw8.Start();

                            betRecordInfo.WinlostAmount = wagerData.winlost_amount;
                            // 走共用流判斷ValidBet、SettlementType、TicketStatus
                            CalculateAvailableBet(wagerData, betRecordInfo);

                            if (wagerData.ticket_extra_status.Equals("cashout", StringComparison.OrdinalIgnoreCase))
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                            }

                            // 只有體育類有Rollover
                            if ((betRecordInfo.GameType != GameType.SBSW_Sports && betRecordInfo.SettlementType == SettlementType.SettledForValid)
                                || sport_type == 157)
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                            }

                            // 主播打賞沒有流水
                            if (sport_type == 157)
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                                betRecordInfo.TicketKey = 0;  //主播打賞不記BA
                            }

                            lgsw8.Stop();
                            settleFlowLog.AppendLine($"4C.Amount Check : {lgsw8.Elapsed}");
                        }
                        else
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, false);
                            settleFlowLog.AppendLine($"{tpUnsettle.TransId} betAmount is not equal to stake, continue next TransId.");
                            continue;
                        }

                        settleFlowLog.AppendLine($"===================================================");
                        settleFlowLog.AppendLine($"5.Ready BetRecordInfo: {JsonConvert.SerializeObject(betRecordInfo)}");
                        settleFlowLog.AppendLine(JsonConvert.SerializeObject(betRecordInfo));
                        settleFlowLog.AppendLine($"===================================================");

                        var lgsw9 = Stopwatch.StartNew();
                        lgsw9.Start();
                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, (TpCategory)tpCategoryId, GetTpSeamlessBonusType.ByTransId);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, true);
                            });
                        }
                        lgsw9.Stop();
                        settleFlowLog.AppendLine($"5A.Execute Settled Function Done: {lgsw9.Elapsed}");
                        settleFlowLog.AppendLine($"===================================================");

                        settleFlowLog.AppendLine($"6.Settled Result is {(result == 1 ? "Success" : "Fail")}");
                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, isSuccess: false);
                        });
                        _logger.LogError(ex, $"SBSW Settle: {ex.Message} || TransId : {tpUnsettle.TransId}");
                    }
                    finally
                    {
                        _logger.LogWarning(settleFlowLog.ToString());
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SBSW Settle exception. {ex.Message}");
            }
            finally
            {
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public enum BetType
        {
            [Description("串关(其他)")] Parlay = 29,
            [Description("单场串关(足球)")] SingleMatchParlay = 38,
            [Description("比分让球盘(足球)")] FootballScoreHandicap = 468,
            [Description("比分大小盘(足球)")] FootballScoreOverUnder = 469
        }

        public enum OddsTypeEnum
        {
            [Description("不属于任何一种市场盘口(仅出现于 sport_type=245，165，200，168，bet_type=468，469)")]
            None = 0,

            [Description("马来盘")]
            MalayOdds = 1,

            [Description("中国盘")]
            ChinaOdds = 2,

            [Description("欧洲盘")]
            DecimalOdds = 3,

            [Description("印度尼西亚盘")]
            IndoOdds = 4,

            [Description("美国盘")]
            AmericanOdds = 5,

            [Description("香港盘")]
            HongKong = 6
        }

        private void CalculateAvailableBet(ISabaBet betDetial, BetRecordInfo betRecordInfo)
        {
            var isEBV = false;

            if (betDetial.ticket_status.Equals("running", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("waiting", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.SettlementType = SettlementType.UnSettled;
                betRecordInfo.ValidBet = 0;
                betRecordInfo.WinlostAmount = 0;
            }
            else if (betDetial.ticket_status.Equals("void", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("reject", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("refund", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("draw", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                betRecordInfo.ValidBet = 0;
            }
            else
            {
                if (betRecordInfo.GameType == GameType.SBSW_CardClub ||
                    betRecordInfo.GameType == GameType.SBSW_Keno ||
                    betRecordInfo.GameType == GameType.SBSW_Lottery ||
                    betRecordInfo.GameType == GameType.SBSW_Vgaming)
                {
                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                    // Saba Club Se Die or Vgaming Se Die gives freebet
                    if ((betDetial.bet_type == 1613 && betDetial.tokenmoney == "freebet") || (betDetial.bet_type == 3803 && betDetial.tokenmoney == "freebet"))
                    {
                        betRecordInfo.ValidBet = 0;
                    }
                    else
                    {
                        betRecordInfo.ValidBet = betRecordInfo.Stake;
                    }
                }
                else
                {
                    isEBV = false;/*Switch EBV*/
                    betRecordInfo.ValidBet = betRecordInfo.Stake;
                    if (betDetial.discount_stake != null && betDetial.discount_stake > 0)
                    {
                        betRecordInfo.ValidBet = betDetial.discount_stake ?? 0;
                    }

                    /*Cause Hard to Win So Cal to Valid*/
                    if ((betDetial.bet_type ?? 0) == (int)BetType.FootballScoreHandicap ||
                        (betDetial.bet_type ?? 0) == (int)BetType.FootballScoreOverUnder ||
                        (betDetial.bet_type ?? 0) == (int)BetType.Parlay ||
                        (betDetial.bet_type ?? 0) == (int)BetType.SingleMatchParlay)
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsTypeEnum.MalayOdds)
                    {
                        if (betDetial.odds < 0 && isEBV)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds);
                        }

                        if (betDetial.odds > 0 && betDetial.odds < 0.7m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsTypeEnum.ChinaOdds)
                    {
                        if (betDetial.odds < 0.7m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsTypeEnum.DecimalOdds)
                    {
                        if (betDetial.odds < 1.7m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsTypeEnum.IndoOdds)
                    {
                        if (betDetial.odds < 0 && isEBV)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds);
                        }

                        if (betDetial.odds < -1.43m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsTypeEnum.AmericanOdds)
                    {
                        if (betDetial.odds < 0)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds) / 100;
                        }

                        if (betDetial.odds < -143m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == 29)
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                    }
                    else
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                    }
                }

                //half won/half lose/won/lose/void/running/draw/reject/refund/waiting
                if (betDetial.ticket_status.StartsWith("half", StringComparison.OrdinalIgnoreCase))
                {
                    betRecordInfo.ValidBet /= 2;
                }
            }

            if (betRecordInfo.SettlementType == SettlementType.UnSettled)
            {
                betRecordInfo.TicketStatus = TicketStatus.UnSettled;
            }
            else if (betDetial.ticket_status.EndsWith("won", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.TicketStatus = TicketStatus.Win;

                if (isEBV)
                {
                    if (Math.Abs(betRecordInfo.ValidBet.Value) > Math.Abs(betRecordInfo.WinlostAmount))
                    {
                        betRecordInfo.ValidBet = Math.Abs(betRecordInfo.WinlostAmount);
                    }
                }
            }
            else if (betDetial.ticket_status.EndsWith("lose", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.TicketStatus = TicketStatus.Lost;
            }
            else if (betDetial.ticket_status.EndsWith("draw", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.TicketStatus = TicketStatus.Draw;
            }
            else
            {
                betRecordInfo.TicketStatus = TicketStatus.Refund;
            }
        }

        private bool isCancelStatus(ISabaBet betDetial)
        {
            return betDetial.ticket_status.Equals("reject", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("refund", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("void", StringComparison.OrdinalIgnoreCase);
        }

        private BaseBetDetail GetWagerData(TpBetRecordDetail tpBetRecordDetail)
        {
            return tpBetRecordDetail.GameType switch
            {
                (int)GameType.SBSW_NumberGame => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetNumberDetails.First(),
                (int)GameType.SBSW_VirtualSports => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetVirtualSportDetails.First(),
                (int)GameType.SBSW_VirtualSports2 => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetVirtualSportDetails.First(),
                (int)GameType.SBSW_Vgaming => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetDetails.First(),
                _ => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetDetails.First(),
            };
        }
    }
}