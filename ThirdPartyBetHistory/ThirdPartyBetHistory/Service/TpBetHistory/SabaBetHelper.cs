using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using ThirdPartyBetHistory.Service.Worker;

namespace ThirdPartyBetHistory.Service.TpBetHistory
{
    public class SabaBetHelper : ThirdPartyBetHelperBase
    {
        public SabaBetHelper(ILogger<WorkerService> logger)
        {
            Logger = logger;
        }
        public interface ISabaBet
        {
            decimal odds { get; set; }
            decimal stake { get; set; }
            public decimal? discount_stake { get; set; }
            public int? odds_type { get; set; }
            public int? bet_type { get; set; }
            public string ticket_status { get; set; }
            string tokenmoney { get; }
        }
        private class Req
        {
            public string vendor_id { set; get; }
            public long version_key { set; get; }
        }
        private class CashOutData
        {
            public long cashout_id { get; set; }
            public DateTime transdate { get; set; }
            public decimal stake { get; set; }
            public decimal winlost_amount { get; set; }
            public decimal buyback_amount { get; set; }
            public decimal real_stake { get; set; }
            public int bet_type { get; set; }
            public string ticket_status { get; set; }
            public DateTime winlost_datetime { get; set; }
        }
        private class BetDetail : ISabaBet
        {
            public long trans_id { get; set; }
            public string vendor_member_id { get; set; }
            public string operator_id { get; set; }
            public int? league_id { get; set; }
            public int? match_id { get; set; }
            public int? home_id { get; set; }
            public int? away_id { get; set; }
            public string match_datetime { get; set; }
            public int? sport_type { get; set; }
            public int? bet_type { get; set; }
            public long? parlay_ref_no { get; set; }
            public decimal odds { get; set; }
            public decimal stake { get; set; }
            public decimal? discount_stake { get; set; }
            public DateTime transaction_time { get; set; }
            public string ticket_status { get; set; }
            public decimal winlost_amount { get; set; }
            public decimal after_amount { get; set; }
            public int currency { get; set; }
            public DateTime winlost_datetime { get; set; }
            public int? odds_type { get; set; }
            public string bet_team { get; set; }
            public string isLucky { get; set; }
            public decimal home_hdp { get; set; }
            public decimal away_hdp { get; set; }
            public decimal? hdp { get; set; }
            public string betfrom { get; set; }
            public string islive { get; set; }
            public int? home_score { get; set; }
            public int? away_score { get; set; }
            public string settlement_time { get; set; }
            public string customInfo1 { get; set; }
            public string customInfo2 { get; set; }
            public string customInfo3 { get; set; }
            public string customInfo4 { get; set; }
            public string customInfo5 { get; set; }
            public string ba_status { get; set; }
            public string tokenmoney { get; set; }
            public int version_key { get; set; }
            public object ParlayData { get; set; }
            public decimal? original_stake { get; set; }
            public IEnumerable<CashOutData> CashOutData { get; set; }
        }
        public class BetNumberDetail : ISabaBet
        {
            public long trans_id { get; set; }
            public string vendor_member_id { get; set; }
            public string operator_id { get; set; }
            public int? match_id { get; set; }
            public DateTime transaction_time { get; set; }
            public decimal odds { get; set; }
            public decimal stake { get; set; }
            public decimal? discount_stake { get; set; }
            public string ticket_status { get; set; }
            public string betfrom { get; set; }
            public string islive { get; set; }
            public int? last_ball_no { get; set; }
            public string bet_team { get; set; }
            public DateTime winlost_datetime { get; set; }
            public int? bet_type { get; set; }
            public int currency { get; set; }
            public int? odds_type { get; set; }
            public int sport_type { get; set; }
            public decimal winlost_amount { get; set; }
            public decimal after_amount { get; set; }
            public string customInfo1 { get; set; }
            public string customInfo2 { get; set; }
            public string customInfo3 { get; set; }
            public string customInfo4 { get; set; }
            public string customInfo5 { get; set; }
            public string ba_status { get; set; }
            public string tokenmoney { get; set; }
            public long version_key { get; set; }
        }
        public class BetVirtualSportDetail : ISabaBet
        {
            public long trans_id { get; set; }
            public string vendor_member_id { get; set; }
            public string operator_id { get; set; }
            public int? league_id { get; set; }
            public int? match_id { get; set; }
            public int? home_id { get; set; }
            public int? away_id { get; set; }
            public string match_datetime { get; set; }
            public int? sport_type { get; set; }
            public int? bet_type { get; set; }
            public long? parlay_ref_no { get; set; }
            public decimal odds { get; set; }
            public decimal stake { get; set; }
            public decimal? discount_stake { get; set; }
            public DateTime transaction_time { get; set; }
            public string ticket_status { get; set; }
            public decimal winlost_amount { get; set; }
            public decimal after_amount { get; set; }
            public int currency { get; set; }
            public DateTime winlost_datetime { get; set; }
            public int? odds_type { get; set; }
            public string bet_team { get; set; }
            public decimal? home_hdp { get; set; }
            public decimal? away_hdp { get; set; }
            public decimal? hdp { get; set; }
            public string betfrom { get; set; }
            public string islive { get; set; }
            public string customInfo1 { get; set; }
            public string customInfo2 { get; set; }
            public string customInfo3 { get; set; }
            public string customInfo4 { get; set; }
            public string customInfo5 { get; set; }
            public string ba_status { get; set; }
            public string tokenmoney { get; set; }
            public long version_key { get; set; }
            public dynamic ParlayData { get; set; }
            public string race_number { get; set; }
            public string race_lane { get; set; }
        }
        private class Data
        {
            public long last_version_key { get; set; }
            public IEnumerable<BetDetail> BetDetails { get; set; }
            public IEnumerable<BetNumberDetail> BetNumberDetails { get; set; }
            public IEnumerable<BetVirtualSportDetail> BetVirtualSportDetails { get; set; }
            public IEnumerable<dynamic> BetCasinoDetails { get; set; }
        }
        private class GetBetDetail
        {
            public int error_code { get; set; }
            public string message { get; set; }
            public Data Data { get; set; }
        }
        public async override Task AddBetRecord(Action<IEnumerable<BetRecordInfo>> action)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var getBetDetailUrl = $"{tpSetting["ApiUrl"]}/GetBetDetail";
            Req req = new Req();
            req.version_key = syncParams.key;
            req.vendor_id = tpSetting["VendorId"];
            var dicP = FromJson<Dictionary<string, string>>(ToJson(req));
            string P = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            var result = await HttpPostAsync(getBetDetailUrl, P, "application/x-www-form-urlencoded");
            var getBetDetail = FromJson<GetBetDetail>(result);

            if (getBetDetail.error_code != 0)
            {
                throw new Exception(getBetDetail.message);
            }
            else
            {
                if (getBetDetail.Data?.BetDetails == null
                    && getBetDetail.Data?.BetNumberDetails == null
                    && getBetDetail.Data?.BetVirtualSportDetails == null
                    && getBetDetail.Data?.BetCasinoDetails == null)
                {
                    syncParams.key = getBetDetail.Data.last_version_key;
                }
                else
                {
                    var betRecordInfoList = new List<BetRecordInfo>();

                    if (getBetDetail.Data?.BetDetails != null)
                    {
                        foreach (var betDetial in getBetDetail.Data?.BetDetails)
                        {
                            var betRecordInfo = new BetRecordInfo
                            {
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
                                betRecordInfo.SettledTime = ConvertLocTime(betDetial.transaction_time > betDetial.winlost_datetime ? betDetial.transaction_time : betDetial.winlost_datetime);
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

                    if (betRecordInfoList.Count == 0)
                    {
                        syncParams.key = getBetDetail.Data.last_version_key;
                    }
                    else
                    {
                        action(betRecordInfoList);
                        syncParams.key = getBetDetail.Data.last_version_key;
                    }
                }
            }
        }

        void CalculateAvailableBet(ISabaBet betDetial, BetRecordInfo betRecordInfo)
        {
            bool isEBV = false;

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
            SyncParams syncParams = new SyncParams();
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
