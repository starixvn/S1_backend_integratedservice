using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.Saba
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
            public int wallet_id { set; get; }
        }

        private class GetBalanceResult : Result
        {
            public List<Datum> Data { get; set; }
        }

        public class Datum
        {
            public string vendor_member_id { get; set; }
            public int balance { get; set; }
            public int bonus_balance { get; set; }
            public int outstanding { get; set; }
            public int currency { get; set; }
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            TransferReg input = new TransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !String.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
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
                if (json.error_code == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = json.Data.trans_id.ToString();
                }
                else if (json.error_code == 1 || json.error_code == 2)
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            CreateMemberReq input = new CreateMemberReq();
            input.vendor_id = inputs.ContainsKey("VendorId") && !String.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.operatorid = null;
            input.userName = input.vendor_member_id;
            input.oddsType = "1";
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            GetBalanceReg input = new GetBalanceReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !String.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.wallet_id = 1;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/CheckUserBalance";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult< UserBalance> oApiResult = new ApiResult<UserBalance>();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<GetBalanceResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = json.Data[0].balance;
                    oApiResult.Data.outstanding = json.Data[0].outstanding;
                }
                else if (json.error_code == 2)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = 0;
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            CreateMemberReq input = new CreateMemberReq();
            input.vendor_id = inputs.ContainsKey("VendorId") && !String.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;

            string url;
            if (appType.ToString() == "Desktop")
                url = inputs.ContainsKey("FrontUrl") && !String.IsNullOrEmpty(inputs["FrontUrl"]) ? inputs["FrontUrl"] : throw new Exception();
            else if (appType.ToString() == "Mobile")
                url = inputs.ContainsKey("MobileUrl") && !String.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception();
            else 
                url = inputs.ContainsKey("MobileUrl") && !String.IsNullOrEmpty(inputs["MobileUrl"]) ? inputs["MobileUrl"] : throw new Exception();

            var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";

            ApiUrl = $"{ApiUrl}/Login";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<LoginResult>(result);
                if (json.error_code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{url}?token={json.Data}&lang={langid}";
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            TransferReg input = new TransferReg();
            input.vendor_id = inputs.ContainsKey("VendorId") && !String.IsNullOrEmpty(inputs["VendorId"]) ? inputs["VendorId"] : throw new Exception();
            input.vendor_member_id = tpCustInfo.TpUserName;
            input.vendor_trans_id = TransID;
            input.amount = Amount;
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
                if (json.error_code == 0)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Data.Status = TransferStatus.Success;
                    apiResult.Data.ThirdId = json.Data.trans_id.ToString();
                }
                else if (json.error_code == 1)
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