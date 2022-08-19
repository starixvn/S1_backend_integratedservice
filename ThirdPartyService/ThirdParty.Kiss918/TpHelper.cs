using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Kiss918
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private static string accessToken = "RUNnMmx6TnJZNjVoTU5ha1JyMCtENFY4bUFyWDVwYW1JbTRlNVRZUTRFY1NjTk1xSFV1WTI3NkNGWHgzSzJPRDZSN041NkJaUlp1WUIxSUhSUE5lSFA2dGhQK2svU2FGVHVOcHY0VnNWeGk5WE01d2U0cFgwbkFVeG9vSnpoZVNobmJSRCtJVVdzRzZCK3luV29zWGRRPT0=";
        private class CreateMemberResponse
        {
            public int playerID { get; set; }
            public string error { get; set; }
            public string description { get; set; }
        }

        private class LoginAccessTokenResponse
        {
            public string actk { get; set; }
            public string status { get; set; }
            public int playerID { get; set; }
            public string description { get; set; }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            ApiUrl = $"{ApiUrl}/api/createplayer";

            var parameters = ToJson(new
            {
                accountID = tpCustInfo.TpUserName,
                nickname = tpCustInfo.TpUserName,
                currency = currency
            });
            var header = new Dictionary<string, string>() { { "token", $"{accessToken}" } };
            var result = HttpPostClient(ApiUrl, parameters, "application/json", header);

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<CreateMemberResponse>(result);
                if (json.error == "0")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    tpCustInfo.TpCustId = json.playerID.ToString(); //Tp CustID (PK)
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.description;
                    oApiResult.RawErrorCode = json.error;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
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
            string currency = tpCustInfo.TpCurrencyId;
            int siteId = tpCustInfo.SiteId;

            string baseApiUrl = inputs.ContainsKey("GameApiUrl") && !string.IsNullOrEmpty(inputs["GameApiUrl"]) ? inputs["GameApiUrl"] : throw new Exception("GameApiUrl");
            string secretKey = inputs.ContainsKey($"SecretKey_{currency}") && !string.IsNullOrEmpty(inputs[$"SecretKey_{currency}"]) ? inputs[$"SecretKey_{currency}"] : throw new Exception($"SecretKey_{currency}");
            string md5EncryptKey = inputs.ContainsKey($"Md5EncryptKey_{currency}") && !string.IsNullOrEmpty(inputs[$"Md5EncryptKey_{currency}"]) ? inputs[$"Md5EncryptKey_{currency}"] : throw new Exception($"Md5EncryptKey_{currency}");
            string encryptKey = inputs.ContainsKey($"EncryptKey_{currency}") && !string.IsNullOrEmpty(inputs[$"EncryptKey_{currency}"]) ? inputs[$"EncryptKey_{currency}"] : throw new Exception($"EncryptKey_{currency}");
            string delimiter = inputs.ContainsKey("Delimiter") && !string.IsNullOrEmpty(inputs["Delimiter"]) ? inputs["Delimiter"] : throw new Exception("Delimiter");
            string siteName = inputs.ContainsKey($"SiteName_{siteId}") && !string.IsNullOrEmpty(inputs[$"SiteName_{siteId}"]) ? inputs[$"SiteName_{siteId}"] : throw new Exception($"SiteName_{siteId}");
            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var loginApiUrl = $"{baseApiUrl}/api/Acc/Login";

            var currTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            #region GameList Sample Code

            //var gapiUrl = $"{baseApiUrl}/api/Game/GameList";

            //var QS = $"key={secretKey}{delimiter}time={currTime}{delimiter}gameType=6";   //1,2,3,4,5,7,8,9
            //var gq = HttpUtility.UrlEncode(DESEncrypt(QS, encryptKey));
            //var gs = BuildMD5(QS + md5EncryptKey + currTime + secretKey);

            //var grequest = ToJson(new
            //{
            //    q = gq,
            //    s = gs,
            //    accessToken = token
            //});

            #endregion

            //vendor建議用字串組，用其他方法會有偶發問題
            var QS = $"key={secretKey}{delimiter}" +
                $"time={currTime}{delimiter}" +
                $"userName={tpCustInfo.TpUserName}@{siteName}{delimiter}" +
                $"password={tpCustInfo.TpPwd}{delimiter}" +
                $"currency={currency}{delimiter}" +
                $"nickName={tpCustInfo.TpUserName}";
            var q = HttpUtility.UrlEncode(DESEncrypt(QS, encryptKey));
            var s = BuildMD5(QS + md5EncryptKey + currTime + secretKey);

            var request = ToJson(new
            {
                q = q,
                s = s,
                accessToken = accessToken
            });

            var result = HttpPostClient(loginApiUrl, request, "application/json");

            ApiResult oApiResult = new ApiResult();

            if (!IsJSON(result))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result"
                };

            var json = JsonConvert.DeserializeObject<LoginAccessTokenResponse>(result);

            if (json.status != "1") //success
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = json.status,
                    RawMessage = json.description
                };

            var joinGameRequest = new
            {
                tkn = json.actk,
                language = TpLangId,
                user = $"{tpCustInfo.TpUserName}@{siteName}",
                gName = gameCode,
                returnUrl = homeUrl
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(joinGameRequest));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{baseApiUrl}/CallGame?{sParam}"
            };
        }

        #region Transfer Method

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            //string currency = tpCustInfo.TpCurrencyId;
            //string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            //string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            //string userName = inputs.ContainsKey($"userName_{currency}") && !string.IsNullOrEmpty(inputs[$"userName_{currency}"]) ? inputs[$"userName_{currency}"] : throw new Exception($"userName_{currency}");
            //string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            //ApiUrl = $"{ApiUrl}/setScore.ashx";

            //long current = GetTimestemp1();
            //string sign = authcode + tpCustInfo.TpUserName + current + secretkey;

            //if (direction == Direction.ToMain)
            //{
            //    Amount = Amount * -1;
            //}

            //var Transfer = new
            //{
            //    action = "setServerScore",
            //    userName = tpCustInfo.TpUserName,
            //    orderid = TransID,
            //    scoreNum = Amount,
            //    ActionUser = userName,
            //    authcode = authcode,
            //    time = current,
            //    sign = ToMD5(sign.ToLower()).ToUpper()
            //};

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(Transfer));
            //var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var result = HttpPost(
            //    ApiUrl,
            //    sParam,
            //    "application/x-www-form-urlencoded"
            //);

            //ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            //if (IsJSON(result))
            //{
            //    var json = JsonConvert.DeserializeObject<TransferResponse>(result);
            //    if (json.code == 0 && json.success == true)
            //    {
            //        apiResult.ErrorCode = ErrorCode.Success;
            //        apiResult.Data.Status = TransferStatus.Success;
            //        // apiResult.Data.ThirdId = TransID;
            //    }
            //    else if (json.code == -1 || json.code == -2 || json.code == -5 || json.code == -7 || json.code == -8 || json.code == -9 || json.code == -99)
            //    {
            //        apiResult.RawMessage = json.msg;
            //        apiResult.RawErrorCode = json.code.ToString();
            //        apiResult.ErrorCode = ErrorCode.Fail;
            //        apiResult.Data.Status = TransferStatus.Fail;
            //    }
            //    else
            //    {
            //        apiResult.RawMessage = json.msg;
            //        apiResult.RawErrorCode = json.code.ToString();
            //        apiResult.ErrorCode = ErrorCode.TransferPending;
            //        apiResult.Data.Status = TransferStatus.Pending;
            //    }
            //}
            //else
            //{
            //    apiResult.RawMessage = "No Result";
            //    apiResult.RawErrorCode = "1";
            //    apiResult.ErrorCode = ErrorCode.TransferPending;
            //    apiResult.Data.Status = TransferStatus.Pending;
            //}

            //return apiResult;

            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            //string currency = tpCustInfo.TpCurrencyId;
            //string WagerUrl = inputs.ContainsKey("WagerUrl") && !string.IsNullOrEmpty(inputs["WagerUrl"]) ? inputs["WagerUrl"] : throw new Exception("WagerUrl");
            //string authcode = inputs.ContainsKey($"authcode_{currency}") && !string.IsNullOrEmpty(inputs[$"authcode_{currency}"]) ? inputs[$"authcode_{currency}"] : throw new Exception($"authcode_{currency}");
            //string secretkey = inputs.ContainsKey($"secretkey_{currency}") && !string.IsNullOrEmpty(inputs[$"secretkey_{currency}"]) ? inputs[$"secretkey_{currency}"] : throw new Exception($"secretkey_{currency}");
            //WagerUrl = $"{WagerUrl}/getOrder.ashx";

            //long current = GetTimestemp1();
            //string sign = authcode + TransID + current + secretkey;

            //var CheckTransfer = new
            //{
            //    orderid = TransID,
            //    authcode = authcode,
            //    time = current,
            //    sign = ToMD5(sign.ToLower()).ToUpper()
            //};

            //var dicP = FromJson<Dictionary<string, string>>(ToJson(CheckTransfer));
            //var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            //var result = HttpPost(
            //    WagerUrl,
            //    sParam,
            //    "application/x-www-form-urlencoded"
            //);

            //ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            //if (IsJSON(result))
            //{
            //    var json = JsonConvert.DeserializeObject<CheckTransferResponse>(result);
            //    if (json.code == 1 && json.success == true)
            //    {
            //        apiResult.ErrorCode = ErrorCode.Success;
            //        apiResult.Data.Status = TransferStatus.Success;
            //        //apiResult.Data.ThirdId = TransID;
            //    }
            //    else if (json.code == -1)
            //    {
            //        apiResult.RawMessage = json.msg ?? "";
            //        apiResult.RawErrorCode = json.code.ToString();
            //        apiResult.ErrorCode = ErrorCode.Fail;
            //        apiResult.Data.Status = TransferStatus.Fail;
            //    }
            //    else
            //    {
            //        apiResult.RawMessage = json.msg;
            //        apiResult.RawErrorCode = json.code.ToString();
            //        apiResult.ErrorCode = ErrorCode.TransferPending;
            //        apiResult.Data.Status = TransferStatus.Pending;
            //    }
            //}
            //else
            //{
            //    apiResult.RawMessage = "No Result";
            //    apiResult.RawErrorCode = "1";
            //    apiResult.ErrorCode = ErrorCode.TransferPending;
            //    apiResult.Data.Status = TransferStatus.Pending;
            //}

            //return apiResult;

            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        #endregion

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #region Private Method

        public static string DESEncrypt(string inString, string key)
        {
            byte[] encryptKey = Encoding.ASCII.GetBytes(key);
            MemoryStream ms = new MemoryStream();
            ICryptoTransform createEncryptor = new DESCryptoServiceProvider().CreateEncryptor(encryptKey, encryptKey);
            CryptoStream cs = new CryptoStream(ms, createEncryptor, CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(cs);
            sw.Write(inString);
            sw.Flush();
            cs.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        public static string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString();
        }

        #endregion

    }
}
