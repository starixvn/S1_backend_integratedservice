using Newtonsoft.Json;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;
using ThirdPartyBetHistory.Service.Model.Interfaces;
using ThirdPartyBetHistory.Service.Library;

namespace ThirdPartyBetHistory.Service.TpBetHistory
{
    public class SabaBetHelper : ThirdPartyBetHelperBase
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Dictionary<int, TpGameTypeOption> _tpGameTypeConfig;
        private readonly List<int> _kenoGameTypeList;

        public SabaBetHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBetHelperBase>>();
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            var tpGameTypeOptions = provider.GetRequiredService<IEnumerable<TpGameTypeOption>>();
            var tpList = provider.GetRequiredService<IEnumerable<TpListOption>>().ToDictionary(d => d.Id, d => d);
            foreach (var tpGameType in tpGameTypeOptions)
            {
                tpGameType.TpName = tpList.TryGetValue(tpGameType.TpId, out var result) ? result.TpName : string.Empty;
            }
            _tpGameTypeConfig = tpGameTypeOptions.ToDictionary(i => i.Type, i => i);
            _kenoGameTypeList = new List<int>
            {
                (int)GameType.SBSW_Keno,  // 202 OW Keno
                (int)GameType.SBSW_Lottery, // 220 OW Lottery
                (int)GameType.SBSW_TableGame, // 222 OW TableGame
            };
        }

        public override async Task UpdateBetRecordByTransId(WorkerType workerType, DateTime startTime, DateTime endTime, IEnumerable<SettlementType> settlementTypeList)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var tickets = _thirdPartyService.GetTpBetRecordReSettleTicket(Tp.SBSW, SiteId, settlementTypeList, startTime, endTime);
            foreach (var ticket in tickets.OrderBy(i => i.SettledTime).ThenBy(i => i.TransId))
            {
                try
                {
                    var api = _kenoGameTypeList.Contains(ticket.GameType) ? "GetKenoDetailByTransID" : "GetBetDetailByTransID";
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
                        var settleDataList = ProcessData(getBetDetail);
                        _thirdPartyBLL.DoSettleTicket(settleDataList, workerType);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"UpdateBetRecordByTransId, TransId={ticket.TransId}");
                }
            }
        }

        public override async Task<long> AddBetRecord(WorkerType workerType, long siteTicketKey)
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

            var settleDataList = ProcessData(getBetDetail);

            if (settleDataList.Count == 0)
            {
                return getBetDetail.Data.last_version_key;
            }

            _thirdPartyBLL.DoSettleTicket(settleDataList, workerType);
            return getBetDetail.Data.last_version_key;
        }

        private bool DataNotExists(GetBetDetail getBetDetail)
        {
            return getBetDetail.Data?.BetDetails == null
                   && getBetDetail.Data?.BetNumberDetails == null
                   && getBetDetail.Data?.BetVirtualSportDetails == null
                   && getBetDetail.Data?.BetCasinoDetails == null;
        }

        private List<SabaSettleData> ProcessData(GetBetDetail getBetDetail)
        {
            var settleDataList = new List<SabaSettleData>();

            if (getBetDetail.Data?.BetDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetDetails)
                {
                    var gameType = GetBetDetailGameType(betDetial.sport_type);
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        GameType = gameType,
                        TransId = betDetial.trans_id.ToString(),
                        TransTime = ConvertLocTime(betDetial.transaction_time),
                        OriginalState = betDetial.ticket_status,
                        WinlostAmount = betDetial.winlost_amount,
                        Stake = betDetial.stake,
                        TpUserName = betDetial.vendor_member_id,
                        TicketKey = betDetial.version_key,
                        tokenmoney = betDetial.tokenmoney,
                        GameID = betDetial.sport_type.ToString()
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

                    // 處理CashOut的注單
                    if (betDetial.ticket_extra_status.Equals("cashout", StringComparison.OrdinalIgnoreCase))
                    {
                        betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                    }

                    var tpBetRecordExtend = new TpBetRecordExtend(_tpGameTypeConfig[(int)gameType], betDetial);
                    settleDataList.Add(new SabaSettleData(betRecordInfo, tpBetRecordExtend));
                }
            }

            if (getBetDetail.Data?.BetNumberDetails != null)
            {
                var gameType = GameType.SBSW_NumberGame;
                foreach (var betDetial in getBetDetail.Data?.BetNumberDetails)
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
                        GameID = betDetial.sport_type.ToString()
                    };

                    CalculateAvailableBet(betDetial, betRecordInfo);
                    var tpBetRecordExtend = new TpBetRecordExtend(_tpGameTypeConfig[(int)gameType], betDetial);
                    settleDataList.Add(new SabaSettleData(betRecordInfo, tpBetRecordExtend));
                }
            }

            if (getBetDetail.Data?.BetVirtualSportDetails != null)
            {
                foreach (var betDetial in getBetDetail.Data?.BetVirtualSportDetails)
                {
                    var betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp,
                        GameType = GameType.SBSW_VirtualSports,
                        OriginalState = betDetial.ticket_status,
                        WinlostAmount = betDetial.winlost_amount,
                        Stake = betDetial.stake,
                        TpUserName = betDetial.vendor_member_id,
                        TicketKey = betDetial.version_key,
                        TransId = betDetial.trans_id.ToString(),
                        TransTime = ConvertLocTime(betDetial.transaction_time),
                        GameID = betDetial.sport_type.ToString()
                    };


                    if (betDetial.sport_type >= 190 && betDetial.sport_type <= 199)
                    {
                        betRecordInfo.GameType = GameType.SBSW_VirtualSports2;
                    }

                    if (betDetial.match_datetime != null)
                    {
                        var settlementTime = DateTime.Parse(betDetial.match_datetime);
                        betRecordInfo.SettledTime = ConvertLocTime(settlementTime);
                    }

                    CalculateAvailableBet(betDetial, betRecordInfo);
                    var tpBetRecordExtend = new TpBetRecordExtend(_tpGameTypeConfig[(int)betRecordInfo.GameType], betDetial);
                    settleDataList.Add(new SabaSettleData(betRecordInfo, tpBetRecordExtend));
                }
            }

            return settleDataList;
        }

        void CalculateAvailableBet(ISabaBet betDetial, BetRecordInfo betRecordInfo)
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

        public override async Task<long> AddTpBetRecordDetail(WorkerType workerType, long siteTicketKey)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var parameters = new Dictionary<string, string>
            {
                { "vendor_id", tpSetting["VendorId"] },
                { "version_key", siteTicketKey.ToString()}
            };

            if (workerType == WorkerType.RngWager)
            {
                return await GetRNGBetDetailToAdd(parameters);
            }
            else
            {
                return await GetTpBetRecordDetailToAdd(parameters);
            }
        }
        private async Task<long> GetTpBetRecordDetailToAdd(Dictionary<string, string> parameters)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var getBetDetailUrl = $"{tpSetting["ApiUrl"]}/GetBetDetail";
            var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

            var result = await HttpPostAsync(getBetDetailUrl, p, "application/x-www-form-urlencoded");
            var getBetDetail = FromJson<GetBetDetail>(result);

            if (getBetDetail.error_code != 0)
            {
                _logger.LogError(getBetDetail.message, $"GetRNGBetDetail");
            }

            if (DataNotExists(getBetDetail))
            {
                return getBetDetail.Data.last_version_key;
            }

            if (getBetDetail.Data?.BetDetails != null)
            {
                foreach (var betDetail in getBetDetail.Data?.BetDetails)
                {
                    var gameType = GetBetDetailGameType(betDetail.sport_type);
                    //VG的資料由GetRNGBetDetailToAdd取得，這邊不重複塞入資料
                    if (gameType == GameType.SBSW_Vgaming)
                        continue;
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, betDetail);
                }
            }
            if (getBetDetail.Data?.BetNumberDetails != null)
            {
                var gameType = GameType.SBSW_NumberGame;
                foreach (var betDetail in getBetDetail.Data?.BetNumberDetails)
                {
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, betDetail);
                }
            }
            if (getBetDetail.Data?.BetVirtualSportDetails != null)
            {
                var gameType = GameType.SBSW_VirtualSports;
                foreach (var betDetail in getBetDetail.Data?.BetVirtualSportDetails)
                {
                    if (betDetail.sport_type >= 190 && betDetail.sport_type <= 199)
                    {
                        gameType = GameType.SBSW_VirtualSports2;
                    }
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, betDetail);
                }
            }
            return getBetDetail.Data.last_version_key;
        }
        private async Task<long> GetRNGBetDetailToAdd(Dictionary<string, string> parameters)
        {
            var tpSetting = thirdPartyModel.TpSetting;
            var getBetDetailUrl = $"{tpSetting["ApiUrl"]}/GetRNGBetDetail";
            var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

            var result = await HttpPostAsync(getBetDetailUrl, p, "application/x-www-form-urlencoded");
            var GetRNGBetDetail = FromJson<GetRNGBetDetail>(result);

            if (GetRNGBetDetail.error_code != 0)
            {
                _logger.LogError(GetRNGBetDetail.message, $"GetRNGBetDetail");
            }

            if (GetRNGBetDetail.Data?.BetRNGDetails is null)
            {
                return GetRNGBetDetail.Data.last_version_key;
            }

            foreach (var betDetail in GetRNGBetDetail.Data.BetRNGDetails)
            {
                ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, GameType.SBSW_Vgaming, betDetail);
            }

            return GetRNGBetDetail.Data.last_version_key;
        }
        private void ProcessTpBetRecordDetailData<T>(string tpUserName, string transId, DateTime settledTime, GameType gameType, T betDetail)
        {
            try
            {
                var tpCustInfo = _thirdPartyBLL.GetTpCustInfoWithCache((int)Tp.SBSW, tpUserName);
                var tpBetRecordDetail = new TpBetRecordDetail((int)Tp.SBSW, gameType, tpCustInfo.CurrencyId, transId, settledTime)
                {
                    Detail = betDetail
                };
                if (_thirdPartyBLL.AddTpBetRecordDetail(tpBetRecordDetail) > 0)
                {
                    _thirdPartyBLL.InsertTpUnsettle(Tp.SBSW, gameType, transId, settledTime);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"AddTpBetRecordDetail, TransId={transId}");
            }
        }
    }
}
