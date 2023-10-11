using ThirdPartyBetHistory.Service.BLL;
using ThirdPartyBetHistory.Service.DAL;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.TpBetHistory
{
    public class SabaBetHelper : ThirdPartyBetHelperBase
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly List<int> _kenoGameTypeList;

        public SabaBetHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBetHelperBase>>();
            _thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _kenoGameTypeList = new List<int>
            {
                (int)GameType.SBSW_Keno,  // 202 OW Keno
                (int)GameType.SBSW_Lottery, // 220 OW Lottery
                (int)GameType.SBSW_TableGame, // 222 OW TableGame
            };
        }

        public override async Task UpdateBetRecordByTransId(WorkerType workerType, DateTime startTime, DateTime endTime, IEnumerable<SettlementType> settlementTypeList)
        {
            var tickets = _thirdPartyService.GetTpBetRecordReSettleTicket(Tp, SiteId, settlementTypeList, startTime, endTime);
            foreach (var ticket in tickets.OrderBy(i => i.SettledTime).ThenBy(i => i.TransId))
            {
                try
                {
                    var api = _kenoGameTypeList.Contains(ticket.GameType) ? "GetKenoDetailByTransID" : "GetBetDetailByTransID";
                    var url = $"{SBSWConfig.ApiUrl}/{api}";
                    var parameters = new Dictionary<string, string>
                    {
                        { "vendor_id", SBSWConfig.VendorId },
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
                        var gameType = (GameType)ticket.GameType;

                        var betDetail = getBetDetail.Data?.BetDetails?.First();
                        if (betDetail != null)
                        {
                            var tpBetRecordDetail = new BetDetailsModel()
                            {
                                last_version_key = getBetDetail.Data.last_version_key,
                                BetDetails = new List<BetDetail>()
                                {
                                    betDetail
                                }
                            };
                            ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                        }

                        var betNumberDetail = getBetDetail.Data?.BetNumberDetails?.First();
                        if (betNumberDetail != null)
                        {
                            var tpBetRecordDetail = new BetNumberDetailModel()
                            {
                                last_version_key = getBetDetail.Data.last_version_key,
                                BetNumberDetails = new List<BetNumberDetail>()
                                {
                                    betNumberDetail
                                }
                            };
                            ProcessTpBetRecordDetailData(betNumberDetail.vendor_member_id, betNumberDetail.trans_id.ToString(), betNumberDetail.transaction_time, gameType, tpBetRecordDetail);
                        }

                        var betVirtualSportDetail = getBetDetail.Data?.BetVirtualSportDetails?.First();
                        if (betVirtualSportDetail != null)
                        {
                            var tpBetRecordDetail = new BetVirtualSportDetailModel()
                            {
                                last_version_key = getBetDetail.Data.last_version_key,
                                BetVirtualSportDetails = new List<BetVirtualSportDetail>()
                                {
                                    betVirtualSportDetail
                                }
                            };
                            ProcessTpBetRecordDetailData(betVirtualSportDetail.vendor_member_id, betVirtualSportDetail.trans_id.ToString(), betVirtualSportDetail.transaction_time, gameType, tpBetRecordDetail);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"UpdateBetRecordByTransId, TransId={ticket.TransId}");
                }
            }
        }

        private bool DataNotExists(GetBetDetail getBetDetail)
        {
            return getBetDetail.Data?.BetDetails == null
                   && getBetDetail.Data?.BetNumberDetails == null
                   && getBetDetail.Data?.BetVirtualSportDetails == null
                   && getBetDetail.Data?.BetCasinoDetails == null;
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
            var parameters = new Dictionary<string, string>
            {
                { "vendor_id", SBSWConfig.VendorId },
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
            var getBetDetailUrl = $"{SBSWConfig.ApiUrl}/GetBetDetail";
            var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));

            var result = await HttpPostAsync(getBetDetailUrl, p, "application/x-www-form-urlencoded");
            var getBetDetail = FromJson<GetBetDetail>(result);

            if (getBetDetail.error_code != 0)
            {
                _logger.LogError(getBetDetail.message, $"GetTpBetRecordDetail");
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
                    //Vgaming的資料由GetRNGBetDetailToAdd取得，這邊不重複塞入資料
                    if (gameType == GameType.SBSW_Vgaming)
                    {
                        continue;
                    }
                    var tpBetRecordDetail = new BetDetailsModel()
                    {
                        last_version_key = getBetDetail.Data.last_version_key,
                        BetDetails = new List<BetDetail>()
                        {
                            betDetail
                        }
                    };
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                }
            }
            if (getBetDetail.Data?.BetNumberDetails != null)
            {
                foreach (var betDetail in getBetDetail.Data?.BetNumberDetails)
                {
                    var gameType = GameType.SBSW_NumberGame;
                    var tpBetRecordDetail = new BetNumberDetailModel()
                    {
                        last_version_key = getBetDetail.Data.last_version_key,
                        BetNumberDetails = new List<BetNumberDetail>()
                        {
                            betDetail
                        }
                    };
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                }
            }
            if (getBetDetail.Data?.BetVirtualSportDetails != null)
            {
                foreach (var betDetail in getBetDetail.Data?.BetVirtualSportDetails)
                {
                    var gameType = GameType.SBSW_VirtualSports;
                    if (betDetail.sport_type >= 190 && betDetail.sport_type <= 199)
                    {
                        gameType = GameType.SBSW_VirtualSports2;
                    }
                    var tpBetRecordDetail = new BetVirtualSportDetailModel()
                    {
                        last_version_key = getBetDetail.Data.last_version_key,
                        BetVirtualSportDetails = new List<BetVirtualSportDetail>()
                        {
                            betDetail
                        }
                    };
                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                }
            }
            return getBetDetail.Data.last_version_key;
        }

        private async Task<long> GetRNGBetDetailToAdd(Dictionary<string, string> parameters)
        {
            var getBetDetailUrl = $"{SBSWConfig.ApiUrl}/GetRNGBetDetail";
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

            foreach (var betDetail in GetRNGBetDetail.Data.BetRNGDetails.Where(x => x.transaction_time > DateTime.Today.AddDays(-10)))
            {
                var rngBetDetail = new BetDetail()
                {
                    trans_id = betDetail.trans_id,
                    vendor_member_id = betDetail.vendor_member_id,
                    transaction_time = betDetail.transaction_time,
                    stake = betDetail.stake,
                    winlost_amount = betDetail.winlost_amount,
                    after_amount = betDetail.after_amount,
                    ticket_status = betDetail.ticket_status,
                    bet_type = betDetail.bet_type,
                    currency = betDetail.currency,
                    version_key = betDetail.version_key
                };
                var tpBetRecordDetail = new BetDetailsModel()
                {
                    last_version_key = GetRNGBetDetail.Data.last_version_key,
                    BetDetails = new List<BetDetail>()
                    {
                        rngBetDetail
                    }
                };
                ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, GameType.SBSW_Vgaming, tpBetRecordDetail);
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

                _thirdPartyBLL.AddTpBetRecordDetail(tpBetRecordDetail, settledTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AddTpBetRecordDetail, TransId={transId}");
            }
        }
    }
}
