using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.NewAMB
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public class CommonResponse
        {
            public Status status { get; set; }
            public Data? data { get; set; }
        }
        public class Status
        {
            public int code { get; set; }
            public string message { get; set; }

        }
        public class Data
        {
            public string url { get; set; }
        }
        
        public class AMBSetting
        {
            public string ApiUrl { get; set; }
            public string Agent { get; set; }
            public string SecretKey { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var ambSetting = GetAMBSetting(inputs);
            var apiUrl = $"{ambSetting.ApiUrl}/seamless/create";

            var param = new
            {
                username = tpCustInfo.TpUserName,
                agent = ambSetting.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-ambslot-signature", HMACSHA512(jsonParam, ambSetting.SecretKey)}
            };

            var result = HttpPost(apiUrl, jsonParam, "application/json", header);

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<CommonResponse>(result);
                if (json.status.code == 0 || json.status.code == 902)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.status.message;
                    oApiResult.RawErrorCode = json.status.code.ToString();
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

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var ambSetting = GetAMBSetting(inputs);
            var apiUrl = $"{ambSetting.ApiUrl}/seamless/launch";
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception();
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            var param = new
            {
                username = tpCustInfo.TpUserName,
                gameId = gameId,
                backLink = homeUrl,
                agent = ambSetting.Agent,
            };

            var jsonParam = JsonConvert.SerializeObject(param);
            var header = new Dictionary<string, string>
            {
                {"x-ambslot-signature", HMACSHA512(jsonParam, ambSetting.SecretKey)}
            };

            var result = HttpPost(
                apiUrl,
                jsonParam,
                "application/json",
                header
            );

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<CommonResponse>(result);
                if (json.status.code == 0)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = $"{json.data.url}&e=disable&h=disable";
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.status.message;
                    oApiResult.RawErrorCode = json.status.code.ToString();
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

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var ambSetting = GetAMBSetting(inputs);
            var roundId = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransPK_AMB_Detail_Get", new { getBetDetail.TransId })
                .GetValueOrDefault("roundId").ToString();
            var sParam = new
            {
                username = tpCustInfo.TpUserName,
                roundId,
                ambSetting.Agent,
                language = "en",
            };

            var param = ToJson(sParam);
            var url = $"{ambSetting.ApiUrl}/seamless/report";
            var header = new Dictionary<string, string>
            {
                { "x-ambslot-signature", HMACSHA512(param, ambSetting.SecretKey) }
            };
            var json = HttpPost(url, param, "application/json", header);
            var result = FromJson<DetailResult>(json);
            if (result.status.code != 0)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status.code} | {result.status.message}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.data.url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public class DetailResult
        {
            public Status status { get; set; }
            public Data data { get; set; }
        }
        
        private AMBSetting GetAMBSetting(IReadOnlyDictionary<string, string> inputs)
        {
            if (!inputs.ContainsKey("ApiUrl") || string.IsNullOrEmpty(inputs["ApiUrl"]) ||
                !inputs.ContainsKey("ApiUrl") || string.IsNullOrEmpty(inputs["ApiUrl"]) ||
                !inputs.ContainsKey("ApiUrl") || string.IsNullOrEmpty(inputs["ApiUrl"]))
            {
                throw new ArgumentException("AMB Setting");
            }

            return new AMBSetting()
            {
                ApiUrl = inputs["ApiUrl"],
                Agent = inputs["Agent"],
                SecretKey = inputs["SecretKey"]
            };
        }

        private static string HMACSHA512(string input, string key)
        {
            byte[] salt = Encoding.UTF8.GetBytes(key);
            int HASH_SIZE = 64;
            int ITERATIONS = 1000;

            // Generate a salt
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            var encoding = new System.Text.UTF8Encoding();

            // Generate the hash
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS, HashAlgorithmName.SHA512);
            var data = pbkdf2.GetBytes(HASH_SIZE);
            return Convert.ToBase64String(data);
        }

    }
}
