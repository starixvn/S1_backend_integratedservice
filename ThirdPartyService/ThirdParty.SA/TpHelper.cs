using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.SA
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private class Result
        {
            public Byte ErrorMsgId { get; set; }
            public string ErrorMsg { get; set; }
        }

        private class LoginResult : Result
        {
            public string Token { set; get; }
            public string DisplayName { set; get; }
        }

        //private class CreateMemberReq
        //{
        //    public string method { set; get; }
        //    public string Key { set; get; }
        //    public string Time { set; get; }
        //    public string Username { set; get; }
        //    public string CurrencyType { set; get; }
        //}

        //private class TransResult : Result
        //{
        //    public Data Data { get; set; }
        //}

        //private class Data
        //{
        //    public bool IsExist { get; set; }
        //    public string Username { get; set; }
        //    public bool IsSuccess { get; set; }
        //    public decimal Balance { get; set; }
        //    public bool Online { get; set; }
        //    public bool Betted { get; set; }
        //    public decimal BettedAmount { get; set; }
        //    public decimal MaxBalance { get; set; }
        //    public decimal MaxWinning { get; set; }
        //    public decimal WithholdAmount { get; set; }
        //    public string BetLimitList { get; set; }
        //    public string BetDetailList { get; set; }
        //    public string Token { get; set; }
        //    public string DisplayName { get; set; }
        //}

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            //CreateMemberReq input = new CreateMemberReq();
            //input.method = "RegUserInfo";
            
            //input.Username = tpCustInfo.TpUserName;
            //input.CurrencyType = tpCustInfo.TpCurrencyId;
            var time = new DateTimeOffset(DateTime.Now);
            time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");
            //input.Time = time.ToString();

            string secretKey = inputs.ContainsKey("SecretKey") && !String.IsNullOrEmpty(inputs["SecretKey "]) ? inputs["SecretKey"] : throw new Exception();
            string encrypKey = inputs.ContainsKey("EncrypKey") && !String.IsNullOrEmpty(inputs["EncrypKey "]) ? inputs["EncrypKey"] : throw new Exception();
            string md5Key = inputs.ContainsKey("MD5Key") && !String.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();
            string QS = $"method=RegUserInfo&Key={secretKey}&Time={time.ToString()}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            string q = HttpUtility.UrlEncode(DESEncrypt(QS, secretKey));
            string s = ToMD5(QS + md5Key + time.ToString() + secretKey);
            //input.Key = s;

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(input)).ToDictionary(kp => kp.Key, kp => kp.Value);
            //string postdata = $"{string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray())}";
            string postdata = "q=" + q + "&s=" + s ;

            ApiUrl = $"{ApiUrl}/api/api.aspx";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsXML(result))
            {
                var xml = XMLDeserialize<Result>(result);

                if (xml.ErrorMsgId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.RawMessage = xml.ErrorMsg;
                    oApiResult.RawErrorCode = xml.ErrorMsgId.ToString();
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
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

       

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string langid, Dictionary<string, string> inputs)
        {
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !String.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();

            string urlKey = $"{appType}Url";
            string url = inputs.ContainsKey(urlKey) && !string.IsNullOrEmpty(inputs[urlKey]) ? inputs[urlKey] : throw new Exception("url Error");

            var time = new DateTimeOffset(DateTime.Now);
            time.ToOffset(TimeSpan.FromHours(8)).ToString("yyyyMMddHHmmss");

            string secretKey = inputs.ContainsKey("SecretKey") && !String.IsNullOrEmpty(inputs["SecretKey "]) ? inputs["SecretKey"] : throw new Exception();
            string encrypKey = inputs.ContainsKey("EncrypKey") && !String.IsNullOrEmpty(inputs["EncrypKey "]) ? inputs["EncrypKey"] : throw new Exception();
            string md5Key = inputs.ContainsKey("MD5Key") && !String.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();
            string QS = $"method=LoginRequest&Key={secretKey}&Time={time.ToString()}&Username={tpCustInfo.TpUserName}&CurrencyType={tpCustInfo.TpCurrencyId}";
            string q = HttpUtility.UrlEncode(DESEncrypt(QS, secretKey));
            string s = ToMD5(QS + md5Key + time.ToString() + secretKey);
            string postdata = "q=" + q + "&s=" + s;

            ApiUrl = $"{ApiUrl}/api/api.aspx";
            var result = HttpPost(ApiUrl, postdata, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsXML(result))
            {
                var xml = XMLDeserialize<LoginResult>(result);

                if (xml.ErrorMsgId == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{url}?token={xml.Token}&lang={langid}";

                    string SportType = null;
                    inputs.TryGetValue("gameId", out SportType);


                    if (appType == AppType.Native)
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.UserName}&token={xml.Token}&lobby={inputs["Lobby"]}&lang={langid}&returnurl={inputs["homeUrl"]}";
                    }
                    else if (appType == AppType.Mobile)
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.UserName}&token={xml.Token}&lobby={inputs["Lobby"]}&lang={langid}&returnurl={inputs["homeUrl"]}";
                    }
                    else
                    {
                        oApiResult.Message = $"{url}?username={tpCustInfo.UserName}&token={xml.Token}&lobby={inputs["Lobby"]}&lang={langid}&returnurl={inputs["homeUrl"]}";
                    }
                }
                else
                {
                    oApiResult.RawMessage = xml.ErrorMsg;
                    oApiResult.RawErrorCode = xml.ErrorMsgId.ToString();
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
            throw new NotImplementedException();
        }
    }
}
