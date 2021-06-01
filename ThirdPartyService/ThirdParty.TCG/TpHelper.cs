using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdParty.TCG
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new TCGConfig(inputs);
            var sParam = new 
            {
                method = "cm",
                username = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd,
                currency = tpCustInfo.TpCurrencyId
            };

            var json = HttpPost(config.ApiUrl, GetBody(sParam, config), ContentType);
            var result = FromJson<TCGResult>(json);

            if (result.status != "0")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = result.error_desc,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new TCGConfig(inputs);
            var sParam = new
            {
                method = "kom",
                username = tpCustInfo.TpUserName,
            };

            var json = HttpPost(config.ApiUrl, GetBody(sParam, config), ContentType);
            var result = FromJson<TCGResult>(json);

            if (result.status != "0")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = result.error_desc,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        private class TCGConfig
        {
            public string ApiUrl { get; set; }
            public string MerchantCode { get; set; }
            public string DesKey { get; set; }
            public string SignKey { get; set; }

            public TCGConfig(Dictionary<string, string> inputs)
            {
                ApiUrl = inputs.TryGetValue("ApiUrl", out string apiUrl) ? apiUrl : throw new Exception($"SignKey is empty");
                MerchantCode = inputs.TryGetValue("MerchantCode", out string merchantCode) ? merchantCode : throw new Exception($"SignKey is empty");
                DesKey = inputs.TryGetValue("DesKey", out string desKey) ? desKey : throw new Exception($"SignKey is empty");
                SignKey = inputs.TryGetValue("SignKey", out string signKey) ? signKey : throw new Exception($"SignKey is empty");
            }
        }

        private class TCGResult
        {
            public string status { get; set; }
            public string error_desc { get; set; }
        }

        private string GetBody(object obj, TCGConfig config)
        {
            var dParam = DesEncrypt(ToJson(obj), config.DesKey);
            var sign = SHA256($"{dParam}{config.SignKey}");
            return $"merchant_code={HttpUtility.UrlEncode(config.MerchantCode)}" +
            $"&params={HttpUtility.UrlEncode(dParam)}" +
            $"&sign={HttpUtility.UrlEncode(sign)}";
        }

        private string DesEncrypt(string input, string key)
        {
            var toEncryptArray =  Encoding.UTF8.GetBytes(input); 
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using var cTransform = des.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        private string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }


    }
}
