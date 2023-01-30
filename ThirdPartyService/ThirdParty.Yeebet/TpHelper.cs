using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Yeebet
{
    public class TpHelper : ThirdPartyHelperBase
    {

        //private readonly Dictionary<string, int> _langList = new() //Yeebet的語系編碼
        //{
        //    {"cs",1 },{ "en",2 },{ "ko",3 },{ "ja",4 },{ "th",5 },
        //    {"zh_TW",6 },{ "vn",10 },{ "id",11 },{ "es",12 },{ "pt",13 },
        //    {"tr",14 }
        //};

        #region Model

        private class LoginResponse
        {
            /// <summary>
            /// 描述
            /// </summary>
            public string desc { get; set; }
            /// <summary>
            /// 结果信息(成功>=0，错误<0，小于零为异常)
            /// </summary>
            public int result { get; set; }
            /// <summary>
            /// 錯誤代碼
            /// </summary>
            public int errid { get; set; }
            /// <summary>
            /// 进入游戏链接
            /// </summary>
            public string openurl { get; set; }
        }

        #endregion

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string apiAppId = inputs.ContainsKey("ApiAppId") && !string.IsNullOrEmpty(inputs["ApiAppId"]) ? inputs["ApiAppId"] : throw new Exception("ApiAppId");
            string secretKey = inputs.ContainsKey("ApiSecretKey") && !string.IsNullOrEmpty(inputs["ApiSecretKey"]) ? inputs["ApiSecretKey"] : throw new Exception("ApiSecretKey");
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            apiUrl = $"{apiUrl}/api/login";

            //var lang = 2; //default en
            //if (_langList.ContainsKey(TpLangId))
            //    lang = _langList[TpLangId];

            var loginRequest = new
            {
                appid = apiAppId,
                clienttype = (int)AppType.Desktop,
                currency = currency,
                iscreate = 1,
                language = int.Parse(TpLangId),
                nickname = tpCustInfo.TpUserName,
                username = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginRequest));
            string sParam = string.Join("&", dicP.Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var sign = BuildMD5(sParam + $"&key={secretKey}");

            var result = HttpPostClient(apiUrl, sParam + $"&sign={sign}", "application/x-www-form-urlencoded");

            if (!IsJSON(result))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result"
                };

            var json = JsonConvert.DeserializeObject<LoginResponse>(result);

            if (json == null || json.result < 0)
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = json == null ? "-1" : json.errid.ToString(),
                    RawMessage = json == null ? "No Result" : json.desc,
                };

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = json.openurl
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
        }

        #region Transfer Methods

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
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

        private static string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString().ToLower();
        }


        #endregion

    }
}