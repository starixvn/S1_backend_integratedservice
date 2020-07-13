using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Saba
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        Dictionary<string, string> sportTypeDic = new Dictionary<string, string>() {
            {"1","soccer" },
            {"2","basketball" },
            {"3","football" },
            {"4","icehockey" },
            {"5","tennis" },
            {"8","baseball" } ,
            {"9","badminton" } ,
            {"43","esports" },
            {"161","numbergame" },
            {"180","virtualsports" },
            {"202","keno" },
            {"208","casino" },
            {"211","allbet" },
            {"220","KenoLottery" },
            {"223","indo4d" },
            {"245","virtualgame" }
        };

        Dictionary<string, string> AllbetlangTypeDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            {"cs","zh_CN" },
            {"ch","zh_TW" },
            {"vn","vi" },
            {"en","en" },
            {"jp","ja" },
            {"ru","ru" },
            {"id","id" },
            {"ko","ko" },
            {"th","th" }
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
                else if (json.error_code == 0 && json.Data.status == 1)
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string langid, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl Error");
            CreateMemberReq input = new CreateMemberReq();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception("vendor_id Error");
            input.vendor_member_id = tpCustInfo.TpUserName;

            string url;
            string urlKey = "";

            urlKey = $"{appType}Url";

            url = inputs.ContainsKey(urlKey) && !string.IsNullOrEmpty(inputs[urlKey]) ? inputs[urlKey] : throw new Exception("url Error");

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/Login";
            if (appType == AppType.Native)
            {
                ApiUrl = $"{inputs["ApiUrl"]}/AppLogIn";
            }
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{url}?token={json.Data}&lang={langid}";

                    string SportType = null;
                    inputs.TryGetValue("gameId", out SportType);

                    if (SportType == "211")
                    {
                        if (AllbetlangTypeDic.ContainsKey(langid))
                        {
                            langid = AllbetlangTypeDic[langid];
                        }
                        else
                        {
                            langid = "en";
                        }
                    }

                    if (appType == AppType.Native)
                    {
                        oApiResult.Message = $"{url}?lang={langid}&mobiletoken={json.Data}";
                    }
                    else if (appType == AppType.Mobile)
                    {
                        string types = $"{SportType},0,l";

                        if (SportType == "202")
                        {
                            types = "Keno";
                        }
                        else if (SportType == "208")
                        {
                            types = "CL";
                        }
                        else if (SportType == "245" || SportType == "43" || SportType == "220" || SportType == "223")
                        {
                            sportTypeDic.TryGetValue(SportType, out types);
                        }

                        if (SportType == "211")
                        {
                            oApiResult.Message = $"{url}?types=AllBet&token={json.Data}&lang={langid}";
                        }
                        else
                        {
                            oApiResult.Message = $"{url}?token={json.Data}&lang={langid}&homeUrl={inputs["homeUrl"]}&signupUrl={inputs["signupUrl"]}&LoginUrl={inputs["LoginUrl"]}&types={types}&isChinaView=true";
                        }
                    }
                    else
                    {
                        string act = null;
                        string gameId = null;

                        if (inputs.TryGetValue("gameId", out gameId))
                        {
                            sportTypeDic.TryGetValue(gameId, out act);
                        }

                        if (act == "keno" || SportType == "43" || SportType == "220" || SportType == "223")
                            oApiResult.Message = $"{url}?token={json.Data}&lang={langid}&game={act}";
                        else
                            oApiResult.Message = $"{url}?token={json.Data}&lang={langid}&act={act}";
                    }
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

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            TransferReg input = new TransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !string.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.vendor_trans_id = TransID;
            input.amount = Amount;
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
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else if (json.error_code == 2 || json.error_code == 3 || json.error_code == 4 || json.error_code == 5 || json.error_code == 6 || json.error_code == 8 || json.error_code == 9 || json.error_code == 10 || json.error_code == 13)
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
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
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else if (json.error_code == 14 || json.error_code == 2 || json.error_code == 3 || json.error_code == 4 || json.error_code == 5 || json.error_code == 6 || json.error_code == 8 || json.error_code == 9 || json.error_code == 10 || json.error_code == 13)
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Status = TransferStatus.Fail;
                }
                else
                {
                    apiResult.RawMessage = json.message;
                    apiResult.RawErrorCode = json.error_code.ToString();
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }
    }
}