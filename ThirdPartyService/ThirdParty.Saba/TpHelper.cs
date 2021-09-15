using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Saba
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyGetBalanceParticular
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

        private class GetBalanceReg : Req
        {
            public string vendor_member_ids { set; get; }
            public int wallet_id { set; get; }
        }

        private class GetBalanceResult : Result
        {
            public List<Datum> Data { get; set; }
        }

        private class SetMemberBetSettingReq : Req
        {
            public string bet_setting { get; set; }
        }
        
        private class BetSetting
        {
            public string sport_type { get; set; }
            public int min_bet { get; set; }
            public int max_bet { get; set; }
            public int max_bet_per_match { get; set; }
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

        private class TransferReg : Req
        {
            public string vendor_trans_id { set; get; }
            public decimal amount { set; get; }
            public int currency { set; get; }
            public int direction { set; get; }
            public int wallet_id { set; get; }
        }

        private class TransResult : Result
        {
            public Data Data { get; set; }
        }

        private class Data
        {
            public int trans_id { get; set; }
            public decimal before_amount { get; set; }
            public decimal after_amount { get; set; }
            public decimal bonus_before_amount { get; set; }
            public decimal bonus_after_amount { get; set; }
            public string system_id { get; set; }
            public int status { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            TransferReg input = new TransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_trans_id = TransID;
            input.wallet_id = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/CheckFundTransfer";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<TransResult>(result);
                if (json.error_code == 0 && json.Data.status == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = json.Data.trans_id.ToString();
                }
                else if (json.error_code == 2)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else if (json.error_code == 0 && json.Data.status == 1)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = apiResult.Message = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            GetBalanceReg input = new GetBalanceReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_ids = tpCustInfo.TpUserName;
            input.wallet_id = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/CheckUserBalance";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<GetBalanceResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = json.Data[0].balance ?? 0;
                    oApiResult.Data.bonus = json.Data[0].bonus_balance ?? 0;
                    oApiResult.Data.outstanding = json.Data[0].outstanding ?? 0;
                }
                else if (json.error_code == 2 || json.error_code == 6)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = 0;
                    oApiResult.Data.bonus = 0;
                    oApiResult.Data.outstanding = 0;
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

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
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
            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            TransferReg input = new TransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.vendor_trans_id = TransID;
            input.amount = Amount;

            if (input.amount <= 0)
            {
                apiResult.RawMessage = "Please submit amount granter than 0";
                apiResult.RawErrorCode = "Please submit amount granter than 0";
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Data.Status = TransferStatus.Fail;
            }

            if (tpCustInfo.IsTest)
                input.currency = 20;
            else
                input.currency = Convert.ToInt32(tpCustInfo.TpCurrencyId);
            input.direction = direction == Direction.ToTp ? 1 : 0;
            input.wallet_id = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/FundTransfer";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<TransResult>(result);
                if (json.error_code == 0 && json.Data.status == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = json.Data.trans_id.ToString();
                }
                else if (json.error_code == 0 && json.Data.status == 1)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else if (json.error_code == 2 || json.error_code == 3 || json.error_code == 4 || json.error_code == 5 || json.error_code == 6 || json.error_code == 8 || json.error_code == 9 || json.error_code == 10 || json.error_code == 13)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = apiResult.Message = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
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
        private class BonusTransferReg : Req
        {
            public string vendor_trans_id { set; get; }
            public int direction
            {
                get
                {
                    return 1;
                }
            }
            public decimal amount { set; get; }
            public int currency { set; get; }
            public decimal locked_amount { set; get; }
            public decimal rollover_amount { set; get; }
            public string expired_date { set; get; }
        }
        public ApiResult<TransferResult> BonusTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal bonus, decimal lockedAmount, decimal rolloverAmount, long expiredDate, string transID, string thirdPartyTransID)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            BonusTransferReg input = new BonusTransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.vendor_trans_id = transID;
            input.amount = bonus;

            if (tpCustInfo.IsTest)
                input.currency = 20;
            else
                input.currency = Convert.ToInt32(tpCustInfo.TpCurrencyId);

            input.locked_amount = lockedAmount;
            input.rollover_amount = rolloverAmount;
            input.expired_date = DateTimeOffset.FromUnixTimeSeconds(expiredDate).ToOffset(new TimeSpan(8, 0, 0)).DateTime.AddDays(1).ToString("yyyy/MM/dd");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/BonusTransfer";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<TransResult>(result);
                if (json.error_code == 0 && json.Data.status == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = json.Data.trans_id.ToString();
                }
                else if (json.error_code == 0 && json.Data.status == 1)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else if (json.error_code == 14 || json.error_code == 2 || json.error_code == 3 || json.error_code == 4 || json.error_code == 5 || json.error_code == 6 || json.error_code == 8 || json.error_code == 9 || json.error_code == 10 || json.error_code == 13)
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = apiResult.Message = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = apiResult.Message = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }

        /// <summary>
        /// 取得指定客戶的餘額資料
        /// </summary>
        /// <param name="inputs">第三方相關參數</param>
        /// <param name="tpUserNameString">第三方使用者名稱參數清單</param>
        /// <returns></returns>
        public ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            GetBalanceReg input = new GetBalanceReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_ids = tpUserNameString;
            input.wallet_id = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/CheckUserBalance";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded", timeOutTime: 300);

            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<GetBalanceResult>(result);
                if (json.error_code == 0)
                {
                    var balanceResult = new List<ReceiveTpUserBalanceModel>();

                    foreach (var data in json.Data)
                    {
                        if (data.error_code == 0 || data.error_code == 2 || data.error_code == 6)
                        {
                            balanceResult.Add(new ReceiveTpUserBalanceModel
                            {
                                ErrorCode = ErrorCode.Success,
                                Message = "ok.",
                                TpUserName = data.vendor_member_id,
                                Balance = data.balance ?? 0
                            });
                        }
                        else
                        {
                            balanceResult.Add(new ReceiveTpUserBalanceModel
                            {
                                ErrorCode = ErrorCode.Fail,
                                Message = "fail.",
                                TpUserName = data.vendor_member_id,
                            });
                        }
                    }

                    return new ApiResult<List<ReceiveTpUserBalanceModel>>()
                    {
                        ErrorCode = ErrorCode.Success,
                        Message = "ok",
                        Data = balanceResult
                    };

                }
                else
                {
                    return new ApiResult<List<ReceiveTpUserBalanceModel>>()
                    {
                        ErrorCode = ErrorCode.Fail,
                        RawErrorCode = json.error_code.ToString(),
                        RawMessage = json.message
                    };
                }
            }
            else
            {
                return new ApiResult<List<ReceiveTpUserBalanceModel>>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawMessage = result
                };
            }

            return new ApiResult<List<ReceiveTpUserBalanceModel>>()
            {
            };
        }

        public ApiResult SetBetLimit(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, int minBet, int maxBet)
        {
            var sportType = new List<string>()
            {
                "1", "2", "3", "5", "8", "10", "11", "43", "99", "99MP", "1MP"
            };
            var apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"])
                ? inputs["ApiUrl"]
                : throw new Exception();
            var input = new SetMemberBetSettingReq();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"])
                ? inputs["VendorId"]
                : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;

            var betSetting = sportType.Select(type => new BetSetting()
            {
                sport_type = type,
                min_bet = minBet,
                max_bet = maxBet,
                max_bet_per_match = maxBet
            }).ToList();
            input.bet_setting = JsonConvert.SerializeObject(betSetting);

            var t = input.GetType();
            var props = t.GetProperties();
            var postData = $"{string.Join("&", props.Select(kp => $"{kp.Name}={kp.GetValue(input)}"))}";

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
                ApiUrl = inputs.GetValueOrDefault("NewApiUrl") ?? inputs.GetValue("ApiUrl");
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
    }
}