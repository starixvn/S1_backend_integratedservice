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

namespace ThirdParty.Saba
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyGetBalanceParticular, IThirdPartyBetLimitParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";
      
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
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
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
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
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

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = new SabaDeepLinkHelper(loginResult.Data, tpCustInfo, appType, TpLangId, inputs).GameUrl
            };          
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        { 
            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            
            try 
            {
                string ApiUrl = new SabaConfig(inputs).ApiUrl;
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
            catch (Exception e)
            {
                apiResult.RawMessage = e.Message;
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
                return apiResult;
            }
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
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
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
        /// <param name="tpCurrencyId">第三方幣別</param>
        /// <returns></returns>
        public ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString, string tpCurrencyId)
        {
            string ApiUrl = new SabaConfig(inputs).ApiUrl;
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
                    RefTransId = gameType == 2 ? betNumberDetail?.trans_id : betDetail?.trans_id,
                    TpUserName = gameType == 2 ? betNumberDetail?.vendor_member_id : betDetail?.vendor_member_id,
                    UTCBetTime = gameType == 2 ? GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) : 
                            GetUTCTime(betDetail?.transaction_time, DateTime.MinValue),
                    UTCSettledTime = gameType == 2 ?　GetUTCTime(betNumberDetail?.transaction_time, DateTime.MinValue) :
                            GetUTCTime(betDetail?.transaction_time, (DateTime)betDetail.transaction_time),
                    Stake = gameType == 2 ? betNumberDetail.stake * tpCurrency.ExchangeRate : betDetail.stake * tpCurrency.ExchangeRate,
                    WinlostAmount = gameType == 2 ? betNumberDetail.winlost_amount * tpCurrency.ExchangeRate : betDetail.winlost_amount * tpCurrency.ExchangeRate,
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
                    Odds = betVirtualSportDetail.odds,
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
