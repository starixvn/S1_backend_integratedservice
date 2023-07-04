using Newtonsoft.Json;
using Serilog.Context;
using StackExchange.Redis;
using System.ComponentModel;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.Options;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using ThirdPartySettleWorker.Model.Exceptions;
using ThirdPartySettleWorker.Model.Interfaces;

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
        private readonly Dictionary<int, SabaSportTypeOption> _sportTypeConfig;

        private readonly ILogger<SBSWWorker> _logger;
        public SBSWWorker(IServiceProvider provider)
        {
            _tpName = Enum.GetName(typeof(Tp), Tp);
            _tpUnSettledBLL = provider.GetRequiredService<TpUnSettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _logger = provider.GetRequiredService<ILogger<SBSWWorker>>();
            _sportTypeConfig = provider.GetRequiredService<IEnumerable<SabaSportTypeOption>>()
                .ToDictionary(item => item.SportType, item => item);
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

            var forContinue = false;
            const string getBettransSp = "Service_Bettrans_SBSW_Bet_Get"; // 取得下注紀錄
            const string getVGBettransSp = "Service_Bettrans_SBSW_Bet_refId_Get"; // 取得下注紀錄(VGAMING)
            const string getSettleSp = "Service_Bettrans_SBSW_Settle_Get"; // 取得未結算的注單

            try
            {
                var tpUnsettleList = _settledBLL.GetSBSWUnsettleList();
                if (!tpUnsettleList.Any())
                {
                    throw new UnsettleNotFoundException($"{_tpName} no unsettle data.");
                }
                forContinue = tpUnsettleList.Count() == 200;

                foreach (var tpUnsettle in tpUnsettleList)
                {
                    try
                    {
                        var tpBetRecordDetail = _settledBLL.GetTpBetRecordDetail((int)Tp.SBSW, (int)tpUnsettle.GameType, tpUnsettle.TransId, tpUnsettle.PartitionTime);
                        var settleData = _tpUnSettledBLL.GetTpUnsettleData<SettleSBSW>(getSettleSp, new { txId = tpUnsettle.TransId }, DataBase.Main);
                        var betData = _tpUnSettledBLL.GetTpUnsettleData<BetSBSW>(tpUnsettle.GameType == GameType.SBSW_Vgaming ? getVGBettransSp : getBettransSp,
                            tpUnsettle.GameType == GameType.SBSW_Vgaming ? new { refId = settleData.FirstOrDefault()?.refId } : new { txId = tpUnsettle.TransId }, DataBase.Main);

                        //取不到下注資料，直接跳過這筆
                        if (betData == null)
                            continue;

                        #region 取得wager資料
                        BaseBetDetail wagerData = tpBetRecordDetail.GameType switch
                        {
                            (int)GameType.SBSW_NumberGame => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetNumberDetails.First(),
                            (int)GameType.SBSW_VirtualSports => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetVirtualSportDetails.First(),
                            (int)GameType.SBSW_VirtualSports2 => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetVirtualSportDetails.First(),
                            (int)GameType.SBSW_Vgaming => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetDetails.First(),
                            _ => JsonConvert.DeserializeObject<GetBetDetailData>(tpBetRecordDetail.Detail).BetDetails.First(),
                        };

                        if (tpUnsettle.GameType == GameType.SBSW_Vgaming)
                            wagerData.sport_type = 171;
                        #endregion

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp, wagerData.vendor_member_id);
                        var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp, tpCustInfo.CurrencyId);

                        if (!_sportTypeConfig.TryGetValue(wagerData.sport_type.Value, out var sabaSportType))
                        {
                            throw new SBSWException(ReturnCode.InvalidParameters, $"sportType:{wagerData.sport_type.Value} not supported.");
                        }


                        #region 匯率轉換
                        wagerData.stake *= tpCurrency.ExchangeRate;
                        wagerData.winlost_amount *= tpCurrency.ExchangeRate;
                        #endregion

                        //未結算預設值
                        var winlostAmount = 0m;
                        var settlementType = SettlementType.UnSettled;
                        var ticketStatus = TicketStatus.UnSettled;
                        var transTime = wagerData.transaction_time;
                        var settledTime = wagerData.transaction_time;
                        var tpCategoryId = sabaSportType.TpCategoryId;
                        var gameType = sabaSportType.TpGameType;

                        var betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp,
                            Stake = wagerData.stake,
                            ValidBet = wagerData.stake,
                            WinlostAmount = winlostAmount,
                            TransId = tpUnsettle.TransId,
                            GameType = (GameType)gameType,
                            TpUserName = wagerData.vendor_member_id,
                            SettledTime = new DateTimeOffset(settledTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds(),
                            TransTime = new DateTimeOffset(transTime, new TimeSpan(-4, 0, 0)).ToUnixTimeSeconds(),
                            SettlementType = settlementType,
                            TicketStatus = ticketStatus,
                            GameID = GetBetDetailGameType(wagerData.sport_type).ToString(),
                            ReferenceNo = settleData.FirstOrDefault()?.refId
                        };

                        if (settleData.Any())
                        {
                            if (betData.Sum(x => x.betAmount) == wagerData.stake)
                            {
                                betRecordInfo.WinlostAmount = wagerData.winlost_amount;
                                CalculateAvailableBet(wagerData, betRecordInfo);

                                if (wagerData.ticket_extra_status.Equals("cashout", StringComparison.OrdinalIgnoreCase))
                                {
                                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                                }

                                /*Only count Sports*/
                                if (betRecordInfo.GameType != GameType.SBSW_Sports && betRecordInfo.SettlementType == SettlementType.SettledForValid)
                                {
                                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                                }
                            }
                            else
                            {
                                LogContext.PushProperty("TP", _tpName);
                                _logger.LogError($"betData=\r\n{JsonConvert.SerializeObject(betData)}\r\nwagerData=\r\n{JsonConvert.SerializeObject(wagerData)}");
                                Task.Run(() =>
                                {
                                    _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, false);
                                });
                                continue;
                            }
                        }

                        var result = _settledBLL.Settled(tpCustInfo, betRecordInfo, (TpCategory)tpCategoryId);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, true);
                            });
                        }

                    }
                    catch (Exception ex)
                    {
                        Task.Run(() =>
                        {
                            _tpUnSettledBLL.SetTpUnsettle(Tp, tpUnsettle.GameType, tpUnsettle.TransId, isSuccess: false);
                        });
                        LogContext.PushProperty("TP", _tpName);
                        _logger.LogError(ex, $"SBSW Settle exception. {ex.Message} || TransId : {tpUnsettle.TransId}");
                    }
                }
            }
            catch (UnsettleNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", _tpName);
                _logger.LogError(ex, $"SBSW Settle exception. {ex.Message}");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(3);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private GameType GetBetDetailGameType(int? sport_type)
        {
            if (sport_type == null)
            {
                return GameType.SBSW_Sports;
            }

            return sport_type switch
            {
                165 => GameType.SBSW_CardClub,
                171 => GameType.SBSW_Vgaming,
                202 => GameType.SBSW_Keno,
                220 => GameType.SBSW_Lottery,
                222 => GameType.SBSW_TableGame,
                _ => GameType.SBSW_Sports,
            };
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
    }
}
