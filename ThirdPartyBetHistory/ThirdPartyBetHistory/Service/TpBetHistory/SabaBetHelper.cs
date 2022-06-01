using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using ThirdPartyBetHistory.Service.Model.Interfaces;

namespace ThirdPartyBetHistory.Service.TpBetHistory
{
    public class SabaBetHelper : ThirdPartyBetHelperBase
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        public SabaBetHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBetHelperBase>>();
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public override async Task UpdateBetRecordByTransId(WorkerType workerType, DateTime today, int hour)
        {
            try
            {
                IEnumerable<UnSettle> tickets;
                switch (workerType)
                {
                    case WorkerType.ByTransIdUnSettleWager:
                        tickets = _thirdPartyService.GetTpBetRecordReSettleTicket(Tp.Saba, SiteId,
                            JsonConvert.SerializeObject(new List<int> { (int)SettlementType.UnSettled }),
                            today.AddDays(-7), today.AddDays(-1).AddSeconds(-1));
                        break;
                    
                    case WorkerType.ByTransIdSettleWager:
                        var startTime = today.AddDays(-1).AddHours(hour);
                        var endTime = startTime.AddHours(1).AddSeconds(-1);
                        var settlementTypeList = JsonConvert.SerializeObject(new List<int>
                        {
                            (int)SettlementType.SettledForValid ,
                            (int)SettlementType.SettledForInvalid,
                            (int)SettlementType.SettledForValidNotRollover
                        });
                        tickets = _thirdPartyService.GetTpBetRecordReSettleTicket(Tp.Saba, SiteId, settlementTypeList, startTime, endTime);
                        break;
                    
                    case WorkerType.MainWager:
                    case WorkerType.LGVirtualSportsWager:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(workerType), workerType, null);
                }

                var tpSetting = thirdPartyModel.TpSetting;

                foreach (var ticket in tickets.OrderBy(i => i.SettledTime).ThenBy(i => i.TransId))
                {
                    try
                    {
                        var api = ticket.GameType == (int)GameType.Saba_Keno ? "GetKenoDetailByTransID" : "GetBetDetailByTransID";
                        var url = $"{tpSetting["ApiUrl"]}/{api}";
                        var parameters = new Dictionary<string, string>
                        {
                            { "vendor_id", tpSetting["VendorId"] },
                            { "trans_id", ticket.TransId}
                        };
                        var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

                        var result = await HttpPostAsync(url, p, "application/x-www-form-urlencoded");
                        var getBetDetail = FromJson<GetBetDetail>(result);
                        if (getBetDetail.error_code != 0)
                        {
                            throw new Exception(getBetDetail.message);
                        }

                        if (!DataNotExists(getBetDetail))
                        {
                            var betRecordInfos = ProcessData(getBetDetail);
                            _thirdPartyBLL.ToDoAddBetRecord(betRecordInfos);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"UpdateBetRecordByTransId, TransId={ticket.TransId}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UpdateBetRecordByTransId");
            }
        }

        public override async Task<long> AddBetRecord(long siteTicketKey)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var getBetDetailUrl = $"{tpSetting["ApiUrl"]}/GetBetDetail";
            var parameters = new Dictionary<string, string>
            {
                { "vendor_id", tpSetting["VendorId"] },
                { "version_key", siteTicketKey.ToString()}
            };
            var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

            var result = await HttpPostAsync(getBetDetailUrl, p, "application/x-www-form-urlencoded");
            var getBetDetail = FromJson<GetBetDetail>(result);

            if (getBetDetail.error_code != 0)
            {
                throw new Exception(getBetDetail.message);
            }

            if (DataNotExists(getBetDetail))
            {
                return getBetDetail.Data.last_version_key;
            }

            var betRecordInfoList = ProcessData(getBetDetail);

            if (betRecordInfoList.Count == 0)
            {
                return getBetDetail.Data.last_version_key;
            }

            _thirdPartyBLL.ToDoAddBetRecord(betRecordInfoList);
            return getBetDetail.Data.last_version_key;
        }

        public async override Task<long> GetVS3rdBetDetail(long siteTicketKey)
        {
            var gameType = GameType.Saba_LGVirtualSports;

            var tpSetting = thirdPartyModel.TpSetting;
            var getBetDetailUrl = $"{tpSetting["ApiUrl"]}/GetVS3rdBetDetail";
            var parameters = new Dictionary<string, string>
            {
                { "vendor_id", tpSetting["VendorId"] },
                { "sport_type", "231" },
                { "version_key", siteTicketKey.ToString()}
            };
            var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));
            var result = await HttpPostAsync(getBetDetailUrl, p, "application/x-www-form-urlencoded");
            var getBetDetail = FromJson<GetVS3rdBetDetail>(result);

            if (getBetDetail.error_code != 0)
            {
                throw new Exception(getBetDetail.message);
            }

            if (!getBetDetail.Data.VS3rdBetDetails.Any())
            {
                return getBetDetail.Data.last_version_key;
            }

            var betRecordInfoList = new List<BetRecordInfo>();

            foreach (var betDetial in getBetDetail.Data?.VS3rdBetDetails)
            {
                var betRecordInfo = new BetRecordInfo
                {
                    TpId = (int)Tp,
                    GameType = gameType,
                    TransId = betDetial.trans_id.ToString(),
                    TransTime = ConvertLocTime(betDetial.transaction_time),
                    SettledTime = ConvertLocTime(betDetial.transaction_time),
                    OriginalState = betDetial.ticket_status,
                    WinlostAmount = betDetial.winlost_amount,
                    Stake = betDetial.stake,
                    TpUserName = betDetial.vendor_member_id,
                    TicketKey = betDetial.version_key,
                    tokenmoney = betDetial.tokenmoney
                };
                CalculateAvailableBet(betDetial, betRecordInfo);
                betRecordInfoList.Add(betRecordInfo);

                var currencyId = _thirdPartyBLL.GetTpCustInfoWithCache((int)Tp, betDetial.vendor_member_id).CurrencyId;
                var detail = new TpBetRecordDetail((int)Tp, gameType, currencyId, betDetial);
                _thirdPartyBLL.AddTpBetRecordDetail(detail);
            }

            if (!betRecordInfoList.Any())
            {
                return getBetDetail.Data.last_version_key;
            }

            _thirdPartyBLL.ToDoAddBetRecord(betRecordInfoList);
            
            return getBetDetail.Data.last_version_key;
        }

        private bool DataNotExists(GetBetDetail getBetDetail)
        {
            return getBetDetail.Data?.BetDetails == null
                   && getBetDetail.Data?.BetNumberDetails == null
                   && getBetDetail.Data?.BetVirtualSportDetails == null
                   && getBetDetail.Data?.BetCasinoDetails == null;
        }

        private List<BetRecordInfo> ProcessData(GetBetDetail getBetDetail)
        {
            var betRecordInfoList = new List<BetRecordInfo>();

            if (getBetDetail.Data?.BetDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetDetails)
                {
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        GameType = GetBetDetailGameType(betDetial.sport_type),
                        TransId = betDetial.trans_id.ToString(),
                        TransTime = ConvertLocTime(betDetial.transaction_time),
                        OriginalState = betDetial.ticket_status,
                        WinlostAmount = betDetial.winlost_amount,
                        Stake = betDetial.stake,
                        TpUserName = betDetial.vendor_member_id,
                        TicketKey = betDetial.version_key,
                        tokenmoney = betDetial.tokenmoney
                    };

                    if (betDetial.settlement_time != null)
                    {
                        var settlementTime = DateTime.Parse(betDetial.settlement_time);
                        betRecordInfo.SettledTime = ConvertLocTime(settlementTime);
                    }
                    else
                    {
                        betRecordInfo.SettledTime = ConvertLocTime(betDetial.transaction_time > betDetial.winlost_datetime
                            ? betDetial.transaction_time
                            : betDetial.winlost_datetime);
                    }

                    CalculateAvailableBet(betDetial, betRecordInfo);
                    betRecordInfoList.Add(betRecordInfo);

                    // 處理CashOut的注單
                    if (TryGetCashOut(betDetial, out var cashout))
                    {
                        betRecordInfoList.AddRange(cashout);
                    }
                }
            }

            if (getBetDetail.Data?.BetNumberDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetNumberDetails)
                {
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        GameType = GameType.Saba_NumberGame,
                        TransId = betDetial.trans_id.ToString(),
                        TransTime = ConvertLocTime(betDetial.transaction_time),
                        SettledTime = ConvertLocTime(betDetial.transaction_time),
                        OriginalState = betDetial.ticket_status,
                        WinlostAmount = betDetial.winlost_amount,
                        Stake = betDetial.stake,
                        TpUserName = betDetial.vendor_member_id,
                        TicketKey = betDetial.version_key
                    };

                    CalculateAvailableBet(betDetial, betRecordInfo);
                    betRecordInfoList.Add(betRecordInfo);
                }
            }

            if (getBetDetail.Data?.BetVirtualSportDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetVirtualSportDetails)
                {
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        OriginalState = betDetial.ticket_status,
                        WinlostAmount = betDetial.winlost_amount,
                        Stake = betDetial.stake,
                        TpUserName = betDetial.vendor_member_id,
                        TicketKey = betDetial.version_key,
                        TransId = betDetial.trans_id.ToString(),
                        TransTime = ConvertLocTime(betDetial.transaction_time),
                    };

                    if (betDetial.sport_type >= 180 && betDetial.sport_type <= 189)
                    {
                        betRecordInfo.GameType = GameType.Saba_VirtualSport;
                    }

                    if (betDetial.sport_type >= 190 && betDetial.sport_type <= 199)
                    {
                        betRecordInfo.GameType = GameType.Saba_VirtualSports2;
                    }

                    if (betDetial.match_datetime != null)
                    {
                        var settlementTime = DateTime.Parse(betDetial.match_datetime);
                        betRecordInfo.SettledTime = ConvertLocTime(settlementTime);
                    }

                    CalculateAvailableBet(betDetial, betRecordInfo);
                    betRecordInfoList.Add(betRecordInfo);
                }
            }

            if (getBetDetail.Data?.BetCasinoDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetCasinoDetails)
                {
                }
            }

            return betRecordInfoList;
        }

        void CalculateAvailableBet(ISabaBet betDetial, BetRecordInfo betRecordInfo)
        {
            var isEBV = false;

            if (betDetial.ticket_status.Equals("running", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("waiting", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.SettlementType = SettlementType.UnSettled;
                betRecordInfo.ValidBet = 0;
            }
            else if (betDetial.ticket_status.Equals("void", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("reject", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("refund", StringComparison.OrdinalIgnoreCase) || betDetial.ticket_status.Equals("draw", StringComparison.OrdinalIgnoreCase))
            {
                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                betRecordInfo.ValidBet = 0;
            }
            else
            {
                if (betRecordInfo.GameType == GameType.Allbet || betRecordInfo.GameType == GameType.PP || betRecordInfo.GameType == GameType.Saba_RNG ||
                    betRecordInfo.GameType == GameType.Saba_CardClub || betRecordInfo.GameType == GameType.Saba_Indo4D || betRecordInfo.GameType == GameType.Saba_Keno ||
                    betRecordInfo.GameType == GameType.Saba_Lottery || betRecordInfo.GameType == GameType.Saba_MacauGame || betRecordInfo.GameType == GameType.Saba_SabaClub ||
                    betRecordInfo.GameType == GameType.Saba_VirtualGames || betRecordInfo.GameType == GameType.Saba_Vgaming)
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

                    if ((betDetial.bet_type ?? 0) == (int)BetType.FootballScoreHandicap ||
                        (betDetial.bet_type ?? 0) == (int)BetType.FootballScoreOverUnder ||
                        (betDetial.bet_type ?? 0) == (int)BetType.Parlay ||
                        (betDetial.bet_type ?? 0) == (int)BetType.FootballScoreOverUnder) /*Cause Hard to Win So Cal to Valid*/
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                    }
                    else if ((betDetial.odds_type ?? 0) == (int)OddsType.MalayOdds)
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
                    else if ((betDetial.odds_type ?? 0) == (int)OddsType.ChinaOdds)
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
                    else if ((betDetial.odds_type ?? 0) == (int)OddsType.DecimalOdds)
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
                    else if ((betDetial.odds_type ?? 0) == (int)OddsType.IndoOdds)
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
                    else if ((betDetial.odds_type ?? 0) == (int)OddsType.AmericanOdds)
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
                    if (Math.Abs(betRecordInfo.ValidBet) > Math.Abs(betRecordInfo.WinlostAmount))
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

        public override SyncParams InitialSyncParams()
        {
            var syncParams = new SyncParams();
            syncParams.key = thirdPartyModel.TicketKey;
            syncParams.Offsets = -4;
            syncParams.timeOfFrequency = 3;
            //syncParams.DoneTime=DateT
            return syncParams;
        }

        private bool TryGetCashOut(BetDetail betDetial, out List<BetRecordInfo> cashOuts)
        {
            cashOuts = default;
            if (betDetial.CashOutData == null || !betDetial.CashOutData.Any())
            {
                return false;
            }

            cashOuts = new List<BetRecordInfo>();
            foreach (var cashOut in betDetial.CashOutData)
            {
                var betRecordInfo = new BetRecordInfo
                {
                    TpId = (int)Tp,
                    TransId = cashOut.cashout_id.ToString(),
                    TransTime = ConvertLocTime(betDetial.transaction_time),
                    SettledTime = ConvertLocTime(cashOut.transdate),
                    Stake = cashOut.stake,
                    WinlostAmount = cashOut.buyback_amount - cashOut.real_stake,
                    OriginalState = cashOut.ticket_status,
                    TpUserName = betDetial.vendor_member_id,
                    TicketKey = betDetial.version_key,
                    tokenmoney = betDetial.tokenmoney,
                    GameType = GetBetDetailGameType(betDetial.sport_type),
                };

                // 使用cashOut的ticket_status狀態跑下面的流程
                betDetial.ticket_status = cashOut.ticket_status;

                // 計算 CashOut的 ValidBet 與設定 SettlementType
                CalculateAvailableBet(betDetial, betRecordInfo);

                cashOuts.Add(betRecordInfo);
            }
            return true;
        }

        private GameType GetBetDetailGameType(int? sport_type)
        {
            if (sport_type == null)
            {
                return GameType.Saba_Sports;
            }

            return sport_type switch
            {
                56 => GameType.Saba_ChinaLottery,
                69 => GameType.Saba_Bitcoin,
                165 => GameType.Saba_CardClub,
                171 => GameType.Saba_Vgaming,
                200 => GameType.Saba_SabaClub,
                202 => GameType.Saba_Keno,
                212 => GameType.Saba_MacauGame,
                220 => GameType.Saba_Lottery,
                222 => GameType.Saba_TableGame,
                223 => GameType.Saba_Indo4D,
                245 => GameType.Saba_VirtualGames,
                211 => GameType.Allbet,
                248 => GameType.PP,
                _ => sport_type > 200 ? GameType.Saba_RNG : GameType.Saba_Sports,
            };
        }
    }
}
