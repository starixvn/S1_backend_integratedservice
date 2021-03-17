using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.TpBetHistory
{
    public class SabaBetHelper : ThirdPartyBetHelperBase
    {
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
                    List<BetRecordInfo> betRecordInfoList = new List<BetRecordInfo>();
                    if (getBetDetail.Data?.BetDetails != null)
                    {
                        foreach (var betDetial in getBetDetail.Data?.BetDetails)
                        {
                            BetRecordInfo betRecordInfo = new BetRecordInfo();
                            betRecordInfo.GameType = GameType.Saba_Sports;
                            if ((betDetial.sport_type ?? 0) == 56)
                            {
                                betRecordInfo.GameType = GameType.Saba_ChinaLottery;
                            }
                            else if ((betDetial.sport_type ?? 0) == 165)
                            {
                                betRecordInfo.GameType = GameType.Saba_CardClub;
                            }
                            else if ((betDetial.sport_type ?? 0) == 200)
                            {
                                betRecordInfo.GameType = GameType.Saba_SabaClub;
                            }
                            else if ((betDetial.sport_type ?? 0) == 202)
                            {
                                betRecordInfo.GameType = GameType.Saba_Keno;
                            }
                            else if ((betDetial.sport_type ?? 0) == 212)
                            {
                                betRecordInfo.GameType = GameType.Saba_MacauGame;
                            }
                            else if ((betDetial.sport_type ?? 0) == 220)
                            {
                                betRecordInfo.GameType = GameType.Saba_Lottery;
                            }
                            else if ((betDetial.sport_type ?? 0) == 222)
                            {
                                betRecordInfo.GameType = GameType.Saba_TableGame;
                            }
                            else if ((betDetial.sport_type ?? 0) == 223)
                            {
                                betRecordInfo.GameType = GameType.Saba_Indo4D;
                            }
                            else if ((betDetial.sport_type ?? 0) == 245)
                            {
                                betRecordInfo.GameType = GameType.Saba_VirtualGames;
                            }
                            else if ((betDetial.sport_type ?? 0) > 200)
                            {
                                betRecordInfo.GameType = betDetial.sport_type switch
                                {
                                    211 => GameType.Allbet,
                                    248 => GameType.PP,
                                    _ => GameType.Saba_RNG
                                };
                            }

                            betRecordInfo.TransId = betDetial.trans_id.ToString();
                            betRecordInfo.TransTime = ConvertLocTime(betDetial.transaction_time);
                            if (betDetial.settlement_time != null)
                            {
                                var settlementTime = DateTime.Parse(betDetial.settlement_time);
                                betRecordInfo.SettledTime = ConvertLocTime(settlementTime);
                            }
                            else
                            {
                                betRecordInfo.SettledTime = ConvertLocTime(betDetial.transaction_time > betDetial.winlost_datetime ? betDetial.transaction_time : betDetial.winlost_datetime);
                            }

                            betRecordInfo.OriginalState = betDetial.ticket_status;
                            betRecordInfo.WinlostAmount = betDetial.winlost_amount;
                            betRecordInfo.Stake = betDetial.stake;
                            betRecordInfo.TpUserName = betDetial.vendor_member_id;
                            betRecordInfo.TicketKey = betDetial.version_key;
                            betRecordInfo.tokenmoney = betDetial.tokenmoney;

                            CalculateAvailableBet(betDetial, betRecordInfo);

                            betRecordInfoList.Add(betRecordInfo);
                        }
                    }

                    if (getBetDetail.Data?.BetNumberDetails != null)
                    {
                        foreach (var betDetial in getBetDetail.Data?.BetNumberDetails)
                        {
                            BetRecordInfo betRecordInfo = new BetRecordInfo();
                            betRecordInfo.GameType = GameType.Saba_NumberGame;
                            betRecordInfo.TransId = betDetial.trans_id.ToString();
                            betRecordInfo.TransTime = ConvertLocTime(betDetial.transaction_time);

                            betRecordInfo.SettledTime = ConvertLocTime(betDetial.transaction_time);

                            betRecordInfo.OriginalState = betDetial.ticket_status;
                            betRecordInfo.WinlostAmount = betDetial.winlost_amount;
                            betRecordInfo.Stake = betDetial.stake;
                            betRecordInfo.TpUserName = betDetial.vendor_member_id;
                            betRecordInfo.TicketKey = betDetial.version_key;

                            CalculateAvailableBet(betDetial, betRecordInfo);
                            betRecordInfoList.Add(betRecordInfo);
                        }
                    }
                    if (getBetDetail.Data?.BetVirtualSportDetails != null)
                    {
                        foreach (var betDetial in getBetDetail.Data?.BetVirtualSportDetails)
                        {
                            BetRecordInfo betRecordInfo = new BetRecordInfo();
                            if (betDetial.sport_type >= 180 || betDetial.sport_type <= 189)
                            {
                                betRecordInfo.GameType = GameType.Saba_VirtualSport;
                            }
                            else
                            {
                                betRecordInfo.GameType = GameType.Saba_VirtualSports2;
                            }

                            betRecordInfo.TransId = betDetial.trans_id.ToString();
                            betRecordInfo.TransTime = ConvertLocTime(betDetial.transaction_time);

                            if (betDetial.match_datetime != null)
                            {
                                var settlementTime = DateTime.Parse(betDetial.match_datetime);
                                betRecordInfo.SettledTime = ConvertLocTime(settlementTime);
                            }

                            betRecordInfo.OriginalState = betDetial.ticket_status;
                            betRecordInfo.WinlostAmount = betDetial.winlost_amount;
                            betRecordInfo.Stake = betDetial.stake;
                            betRecordInfo.TpUserName = betDetial.vendor_member_id;
                            betRecordInfo.TicketKey = betDetial.version_key;

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
                    betRecordInfo.GameType == GameType.Saba_VirtualGames)
                {

                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                    if (betDetial.bet_type==1613 && betDetial.tokenmoney =="freebet")
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

                    if ((betDetial.bet_type ?? 0) == 468 || (betDetial.bet_type ?? 0) == 469) /*Cause Hard to Win So Cal to Valid*/
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                    }
                    else if ((betDetial.odds_type ?? 0) == 1)
                    {
                        if (betDetial.odds < 0 && isEBV)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds);
                        }

                        if (betDetial.odds > 0 && betDetial.odds < 0.5m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == 2)
                    {
                        if (betDetial.odds < 0.5m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == 3)
                    {
                        if (betDetial.odds < 1.5m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == 4)
                    {
                        if (betDetial.odds < 0 && isEBV)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds);
                        }

                        if (betDetial.odds < -2m)
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        }
                        else
                        {
                            betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        }
                    }
                    else if ((betDetial.odds_type ?? 0) == 5)
                    {
                        if (betDetial.odds < 0)
                        {
                            betRecordInfo.ValidBet = betRecordInfo.Stake * Math.Abs(betDetial.odds) / 100;
                        }

                        if (betDetial.odds < -200m)
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
    }
}
