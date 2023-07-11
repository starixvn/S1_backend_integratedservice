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
        private readonly MySqlClient _mainDbClient;

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
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
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
                        var betDetail = getBetDetail.Data?.BetDetails?.First();
                        var betNumberDetail = getBetDetail.Data?.BetNumberDetails?.First();
                        var betVirtualSportDetail = getBetDetail.Data?.BetVirtualSportDetails?.First();
                        var gameType = (GameType)ticket.GameType;
                        if (betDetail != null)
                        {
                            var tpBetRecordDetail = new BetDetailsModel() { last_version_key = getBetDetail.Data.last_version_key };
                            tpBetRecordDetail.BetDetails.Add(betDetail);
                            ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                        }
                        if (betNumberDetail != null)
                        {
                            var tpBetRecordDetail = new BetNumberDetailModel() { last_version_key = getBetDetail.Data.last_version_key };
                            tpBetRecordDetail.BetNumberDetails.Add(betNumberDetail);
                            ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                        }
                        if(betVirtualSportDetail != null)
                        {
                            if (betVirtualSportDetail.sport_type >= 190 && betVirtualSportDetail.sport_type <= 199)
                            {
                                gameType = GameType.SBSW_VirtualSports2;
                            }
                            var tpBetRecordDetail = new BetVirtualSportDetailModel() { last_version_key = getBetDetail.Data.last_version_key };
                            tpBetRecordDetail.BetVirtualSportDetails.Add(betVirtualSportDetail);
                            ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
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

                    //VG的資料由GetRNGBetDetailToAdd取得，這邊不重複塞入資料
                    if (gameType == GameType.SBSW_Vgaming)
                        continue;
                    List<BetDetail> tempBetDetails = new List<BetDetail>();
                    tempBetDetails.Add(betDetail);
                    var tpBetRecordDetail = new BetDetailsModel() { last_version_key = getBetDetail.Data.last_version_key, BetDetails = tempBetDetails };

                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
                }
            }
            if (getBetDetail.Data?.BetNumberDetails != null)
            {
                var gameType = GameType.SBSW_NumberGame;
                foreach (var betDetail in getBetDetail.Data?.BetNumberDetails)
                {
                    List<BetNumberDetail> tempBetNumberDetails = new List<BetNumberDetail>();
                    tempBetNumberDetails.Add(betDetail);
                    var tpBetRecordDetail = new BetNumberDetailModel() { last_version_key = getBetDetail.Data.last_version_key, BetNumberDetails = tempBetNumberDetails };

                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
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
                    List<BetVirtualSportDetail> tempBetVirtualSportDetails = new List<BetVirtualSportDetail>();
                    tempBetVirtualSportDetails.Add(betDetail);
                    var tpBetRecordDetail = new BetVirtualSportDetailModel() { last_version_key = getBetDetail.Data.last_version_key , BetVirtualSportDetails  = tempBetVirtualSportDetails };

                    ProcessTpBetRecordDetailData(betDetail.vendor_member_id, betDetail.trans_id.ToString(), betDetail.transaction_time, gameType, tpBetRecordDetail);
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


            foreach (var betDetail in GetRNGBetDetail.Data.BetRNGDetails.Where(x=> x.transaction_time > DateTime.Today.AddDays(-10)))
            {
                var tempBetDetail = new BetDetail()
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
                List<BetDetail> tempBetDetails = new List<BetDetail>();
                tempBetDetails.Add(tempBetDetail);
                var tpBetRecordDetail = new BetDetailsModel() { last_version_key = GetRNGBetDetail.Data.last_version_key, BetDetails = tempBetDetails };
                
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
            catch(Exception ex)
            {
                _logger.LogError(ex, $"AddTpBetRecordDetail, TransId={transId}");
            }
        }
    }
}
