using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.SBSW
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyBetLimitParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";

        private readonly Dictionary<string, string> sportTypeDic = new Dictionary<string, string>()
        {
            {"1","soccer" },
            {"2","basketball" },
            {"3","football" },
            {"4","icehockey" },
            {"5","tennis" },
            {"6","volleyball" },
            {"8","baseball" } ,
            {"9","badminton" } ,
            {"43","esports" },
            {"50", "indiancricket" },
            {"161","numbergame" },
            {"164","164" },
            {"171","vgaming" },
            {"180","180" },
            {"190","190" },
            {"192","192" },
            {"193","193" },
            {"194","194" },
            {"197","197" },
            {"200","vgaming" },
            //{"200","sabaclub" },
            {"202","keno" },
            {"208","casino" },
            {"220","KenoLottery" },
            {"222","tablegame" },
            {"223","indo4d" },
            {"245","virtualgame" },
            {"997","90088,89692,89697,93062,95730,96666,95731" }
        };

        private readonly Dictionary<string, string> menuTypeDic = new Dictionary<string, string>()
        {
            {"2001","1" },// Olympics
            {"2002","2" },// World Cup
            {"2003","3" } // Euro Cup
        };

        private class Result
        {
            public int error_code { get; set; }
            public string message { get; set; }
        }

        private class Req
        {
            public string vendor_id { set; get; }
            public string vendor_member_id { set; get; }
        }

        private class CreateMemberReq : Req
        {
            public string operatorid { set; get; }
            public string userName { set; get; }
            public string oddsType { set; get; }
            public int currency { set; get; }
            public decimal maxtransfer { set; get; }
            public decimal mintransfer { set; get; }
        }

        private class LoginResult : Result
        {
            public string Data { set; get; }
        }

        private class SetMemberBetSettingReq : Req
        {
            public string bet_setting { get; set; }
        }
        
        public class GetBetSettingLimitReq
        {
            public string vendor_id { set; get; }
            public string currency { set; get; }
        }
           
        private class SetMemberBetSettingResult
        {
            public int error_code { get; set; }
            public string message { get; set; }
        }
        
        public class Datum
        {
            public string vendor_member_id { get; set; }
            public decimal? balance { get; set; }
            public decimal? bonus_balance { get; set; }
            public decimal? outstanding { get; set; }
            public int? currency { get; set; }
            public int error_code { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
            CreateMemberReq input = new CreateMemberReq();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.operatorid = null;
            input.userName = input.vendor_member_id;
            input.oddsType = "1";
            if (tpCustInfo.IsTest)
                input.currency = 20;
            else
                input.currency = Convert.ToInt32(tpCustInfo.TpCurrencyId);
            input.maxtransfer = int.MaxValue;
            input.mintransfer = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/CreateMember";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<Result>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult<UserBalance>()
            {
                ErrorCode = ErrorCode.Fail,
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new TransIdModel
            {
                Implemented = false
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = new SabaConfig(inputs);
            var platform = (int)appType;
            if (inputs.TryGetValue("device", out var device) && device.Equals("wap", StringComparison.OrdinalIgnoreCase))
            {
                platform = 3;
            }

            // 取得進入網址         
            var loginResult = GetSabaUrl(config, tpCustInfo.TpUserName, platform);
            if (loginResult.error_code != 0)
            {
                return new ApiResult()
                {
                    RawMessage = loginResult.message,
                    RawErrorCode = loginResult.error_code.ToString(),
                    ErrorCode = ErrorCode.Fail
                };
            }

            // 設定共用參數
            var resultUrl = new StringBuilder(loginResult.Data);
            resultUrl.Append($"{(tpCustInfo.TpUserName == null ? "?" : "&")}lang={TpLangId}");
            var homeUrl = inputs.GetValueOrDefault("homeUrl");
            var signupUrl = inputs.GetValueOrDefault("signupUrl");
            var loginUrl = inputs.GetValueOrDefault("LoginUrl");
            resultUrl.Append($"&homeUrl={homeUrl}&signupUrl={signupUrl}&LoginUrl={loginUrl}");

            // 依裝置類型設定Deeplink
            var sportType = inputs.GetValueOrDefault("gameId");
            var menuType = inputs.GetValueOrDefault("menutype") ?? "0";
            var matchId = inputs.GetValueOrDefault("matchid");
            var betType = inputs.GetValueOrDefault("bettype");
            if (sportType == "2001" || sportType == "2002" || sportType == "2003")
            {
                sportType = "1";
                menuType = menuTypeDic[sportType];
            }

            var deeplink = appType switch
            {
                AppType.Desktop => GetDesktopDeeplink(sportType, menuType, matchId, betType),
                AppType.Mobile => GetMobileDeeplink(sportType, menuType, matchId, betType),
                AppType.Native => GetNativeDeeplink(matchId, betType),
                _ => string.Empty,
            };

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = resultUrl.Append(deeplink).ToString(),
            };
        }

        private bool CheckSportType(string sportType)
        {
            return sportType switch
            {
                "43" => true,   // 电子竞技
                "171" => true,  // Vgaming
                "200" => true,  // 沙巴俱乐部
                "202" => true,  // 快乐彩
                "220" => true,  // 彩票(Keno Lottery)
                "222" => true,  // 桌面游戏
                "223" => true,  // Togel 4D
                "245" => true,  // 虚拟娱乐城
                _ => false,
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
            Req input = new Req();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/KickUser";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult apiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<Result>(result);
                if (json.error_code == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
            }

            return apiResult;
        }    

        public ApiResult SetBetLimit(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, string betSetting)
        {
            var config = new SabaConfig(inputs);
            var apiUrl = config.ApiUrl;
            var req = new SetMemberBetSettingReq
            {
                vendor_id = config.VendorId, 
                vendor_member_id = tpCustInfo.TpUserName,
                bet_setting = betSetting
            };
            var t = req.GetType();
            var props = t.GetProperties();
            var postData = $"{string.Join("&", props.Select(kp => $"{kp.Name}={kp.GetValue(req)}"))}";

            apiUrl = $"{apiUrl}/SetMemberBetSetting";
            var result = HttpPost(apiUrl, postData, "application/x-www-form-urlencoded");

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<SetMemberBetSettingResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = json.message;
                    oApiResult.RawErrorCode = json.error_code.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        public ApiResult<List<BetSetting>> GetBetLimit(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new SabaConfig(inputs);
            var domain = config.ApiUrl;
            var req = new GetBetSettingLimitReq
            {
                vendor_id = config.VendorId, 
                currency = tpCustInfo.TpCurrencyId
            };
            var t = req.GetType();
            var props = t.GetProperties();
            var postData = $"{string.Join("&", props.Select(kp => $"{kp.Name}={kp.GetValue(req)}"))}";
            
            var apiUrl = $"{domain}/GetBetSettingLimit";
            var result = HttpPost(apiUrl, postData, "application/x-www-form-urlencoded");
            
            var oApiResult = new ApiResult<List<BetSetting>>();
            if (IsJSON(result))
            {
                var betSettingLimit = JsonConvert.DeserializeObject<BetSettingLimit>(result);
                if (betSettingLimit.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data = betSettingLimit.Data;
                }
                else
                {
                    oApiResult.RawMessage = betSettingLimit.message;
                    oApiResult.RawErrorCode = betSettingLimit.error_code.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result;
            }

            return oApiResult;
        }

        private LoginResult GetSabaUrl(SabaConfig config, string tpUserName, int platform)
        {
            var apiUrl = $"{config.ApiUrl}/GetSabaUrl";
            var sParam = $"vendor_id={config.VendorId}&vendor_member_id={tpUserName}&platform={platform}";
            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<LoginResult>(json);
            return result;
        }

        public class SabaConfig
        {
            public string ApiUrl { get; set; }
            public string VendorId { get; set; }

            public SabaConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.GetValue("ApiUrl");
                VendorId = inputs.GetValue("VendorId");
            }
        }

        private string GetDesktopDeeplink(string sportType, string menuType, string matchId, string betType)
        {
            var act = sportType != null ? sportTypeDic.GetValueOrDefault(sportType) : null;
            if (CheckSportType(sportType))
            {
                if (!string.IsNullOrEmpty(betType))
                {
                    return $"&game={act}&gameid={betType}";
                }
                if (sportType == "997")
                {
                    // Saba Soccer deeplink
                    return $"&menutype=0&sportid=1&leaguekey={act}";
                }
                return $"&game={act}";
            }

            if (!string.IsNullOrEmpty(matchId) && matchId != "0")
            {
                return $"&act={act}&matchid={matchId}";
            }
            if (!string.IsNullOrEmpty(betType))
            {
                return $"&act={act}&gameid={betType}&menutype={menuType}";
            }
            if (sportType == "997")
            {
                // Saba Soccer deeplink
                return $"&menutype=0&act=1&leaguekey={act}";
            }

            return $"&act={act}&menutype={menuType}";
        }

        private string GetMobileDeeplink(string sportType, string menuType, string matchId, string betType)
        {
            var types = CheckSportType(sportType) ? sportTypeDic.GetValueOrDefault(sportType) : $"{sportType},{menuType},l";
            if (sportType == "202")
            {
                types = "Keno";
            }
            if (sportType == "208")
            {
                types = "CL";
            }

            var result = $"&types={types}&isChinaView=true";
            if (!string.IsNullOrEmpty(matchId) && matchId != "0")
            {
                return $"{result}&matchid={matchId}";
            }
            if (!string.IsNullOrEmpty(betType))
            {
                return $"{result}&gameid={betType}";
            }

            return result;
        }

        private string GetNativeDeeplink(string matchId, string betType)
        {
            if (!string.IsNullOrEmpty(matchId) && matchId != "0")
            {
                return $"&matchid={matchId}";
            }

            if (!string.IsNullOrEmpty(betType))
            {
                return $"&gameid={betType}";
            }

            return string.Empty;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {  
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var gameType = getBetDetail.TpGameTypeOption.Type;
            var tpBetRecordDetail = TpBetRecordProvider.GetTpBetRecordDetail(getBetDetail);

            if (tpBetRecordDetail == null)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
            }

            var model = JsonConvert.DeserializeObject<DetailModel>(tpBetRecordDetail.Detail);
            var betDetail = model?.BetDetails?.First();
            var betNumberDetail = model?.BetNumberDetails?.First();
            var betVirtualSportDetail = model?.BetVirtualSportDetails?.First();

            if (betDetail == null && betNumberDetail == null && betVirtualSportDetail == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                };
            }

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                    TpUserName = betDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betDetail?.settlement_time, DateTime.MinValue),
                    Odds = betDetail?.odds,
                    Stake = betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betDetail.winlost_amount * tpCurrency.ExchangeRate,
                    LeagueName = betDetail?.leaguename?.First()?.name,
                    HomeTeamName = betDetail?.hometeamname?.First()?.name,
                    AwayTeamName = betDetail?.awayteamname?.First()?.name,
                    HomeHandicap = betDetail?.home_hdp,
                    AwayHandicap = betDetail?.away_hdp,
                    Handicap = betDetail?.hdp,
                    TicketStatus = betDetail?.ticket_status,
                    AwayScore = betDetail?.away_score,
                    HomeyScore = betDetail?.home_score,
                    RefTransId = betDetail?.parlay_ref_no,
                },
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                    RefTransId = betDetail?.trans_id,
                    TpUserName = betDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betDetail?.settlement_time, (DateTime)betDetail?.transaction_time),
                    Stake = betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betDetail.winlost_amount * tpCurrency.ExchangeRate,
                    GameCode = string.Empty,
                    GameName = string.Empty,
                },
                TpCategory.Lottery => new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                    RefTransId = gameType == 70 ? betNumberDetail?.trans_id : betDetail?.trans_id,
                    TpUserName = gameType == 70 ? betNumberDetail?.vendor_member_id : betDetail?.vendor_member_id,
                    UTCBetTime = gameType == 70 ? GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) : 
                            GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = gameType == 70 ?　GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) :
                            GetUTCTime(betDetail?.transaction_time, (DateTime)betDetail.transaction_time),
                    Stake = gameType == 70 ? betNumberDetail.stake * tpCurrency.ExchangeRate : betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = gameType == 70 ? betNumberDetail.winlost_amount * tpCurrency.ExchangeRate : betDetail.winlost_amount * tpCurrency.ExchangeRate,
                },
                TpCategory.VirtualSports => new VirtualSportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                    RefTransId = betVirtualSportDetail?.trans_id,
                    TpUserName = betVirtualSportDetail?.vendor_member_id,
                    UTCBetTime = GetUTCTime(betVirtualSportDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = GetUTCTime(betVirtualSportDetail?.match_datetime, (DateTime)betVirtualSportDetail?.transaction_time),
                    Stake = betVirtualSportDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = betVirtualSportDetail.winlost_amount * tpCurrency.ExchangeRate,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = tpBetRecordDetail.Detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        private DateTime GetUTCTime(DateTime? target, DateTime defaultTime)
        {
            return new DateTimeOffset(target ?? defaultTime, new TimeSpan(-4, 0, 0)).DateTime;
        }

    }
}