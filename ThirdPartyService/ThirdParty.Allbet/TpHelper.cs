using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.Allbet
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private static byte[] NULL_IV = Convert.FromBase64String("AAAAAAAAAAA=");
        public class Req
        {
            public string random { get; set; }
            public string client { get; set; }
            public string password { get; set; }
        }

        public class UserReq : Req
        {
            public string agent { get; set; }
            public int orHallRebate { get; set; }
            public string orHandicapNames { get; set; }
            public string vipHandicapNames { get; set; }
        }

        public class LoginReq : Req
        {
            public string language { get; set; }
            public string returnUrl { get; set; }
        }

        public class Result
        {
            public string error_code { get; set; }
            public string message { get; set; }
        }
        public class UserResult : Result
        {
            public string client { get; set; }
        }
        public class LoginResult : Result
        {
            public string gameLoginUrl { get; set; }
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //throw new NotImplementedException();
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string agentKey = $"{tpCustInfo.TpCurrencyId}Agent";
            string Agent = inputs.ContainsKey(agentKey) && !string.IsNullOrEmpty(inputs[agentKey]) ? inputs[agentKey] : throw new Exception("Can't get agent.");
            string PropertyId = inputs.ContainsKey("PropertyId") && !string.IsNullOrEmpty(inputs["PropertyId"]) ? inputs["PropertyId"] : throw new Exception();
            string DesKey = inputs.ContainsKey("DesKey") && !string.IsNullOrEmpty(inputs["DesKey"]) ? inputs["DesKey"] : throw new Exception();
            string MD5Key = inputs.ContainsKey("MD5Key") && !string.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();

            string sParam = "";
            UserReq userReq = new UserReq();

            userReq.random = random();
            userReq.agent = Agent;
            userReq.client = tpCustInfo.TpUserName;
            userReq.password = tpCustInfo.TpPwd;
            userReq.orHallRebate = 0;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(userReq));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            string data = encrypt(sParam, DesKey, null);
            string sign = base64edMd5($"{data}{MD5Key}");
            sParam = $"{sParam}&propertyId={PropertyId}";
            sParam = $"{sParam}&data={System.Web.HttpUtility.UrlEncode(data)}&sign={System.Web.HttpUtility.UrlEncode(sign)}";

            var result = HttpPost($"{ApiUrl}/create_client", sParam, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<UserResult>(result);
                if (userResult.error_code.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = userResult.error_code;
                    oApiResult.RawErrorCode = userResult.error_code;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.SystemError;
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
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception();
            string PropertyId = inputs.ContainsKey("PropertyId") && !string.IsNullOrEmpty(inputs["PropertyId"]) ? inputs["PropertyId"] : throw new Exception();
            string DesKey = inputs.ContainsKey("DesKey") && !string.IsNullOrEmpty(inputs["DesKey"]) ? inputs["DesKey"] : throw new Exception();
            string MD5Key = inputs.ContainsKey("MD5Key") && !string.IsNullOrEmpty(inputs["MD5Key"]) ? inputs["MD5Key"] : throw new Exception();
            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            string sParam = "";
            LoginReq loginReq = new LoginReq();

            loginReq.random = random();
            loginReq.client = tpCustInfo.TpUserName;
            loginReq.password = tpCustInfo.TpPwd;
            loginReq.language = TpLangId;
            loginReq.returnUrl = homeUrl;

            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            string data = encrypt(sParam, DesKey, null);
            string sign = base64edMd5($"{data}{MD5Key}");
            sParam = $"{sParam}&propertyId={PropertyId}";
            sParam = $"{sParam}&data={System.Web.HttpUtility.UrlEncode(data)}&sign={System.Web.HttpUtility.UrlEncode(sign)}";

            var result = HttpPost($"{ApiUrl}/forward_game", sParam, "application/x-www-form-urlencoded");
            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<LoginResult>(result);
                if (userResult.error_code.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = userResult.gameLoginUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = userResult.error_code;
                    oApiResult.RawErrorCode = userResult.error_code;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.SystemError;
            }
            return oApiResult;
        }
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_Allbet_Detail_Get", new { getBetDetail.TransId });
            var detail = ToJson(wagerDetail);
            var gameCode = wagerDetail.GetValueOrDefault("gameType")?.ToString();
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.LiveCasino => new LiveCasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("gameRoundId")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("client")?.ToString(),
                    UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("betTime", DateTime.MinValue),
                    UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("gameRoundEndTime", DateTime.MinValue),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winOrLoss") * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };  
        }

        private string random()
        {
            using RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
            byte[] byteCsp = new byte[5];
            csp.GetBytes(byteCsp);
            return BitConverter.ToString(byteCsp);
        }

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="dataBytes"></param>
        /// <param name="keyBytes"></param>
        /// <param name="ivBytes"></param>
        /// <returns></returns>
        public static string encrypt(string data, string base64Key, string base64edIv)
        {
            TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
            des.Mode = CipherMode.CBC;
            des.BlockSize = 64;
            des.Padding = PaddingMode.PKCS7;
            des.Key = Convert.FromBase64String(base64Key);
            des.IV = base64edIv == null ? NULL_IV : Convert.FromBase64String(base64edIv);

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using MemoryStream stream = new MemoryStream();
            using CryptoStream encStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);
            encStream.Write(dataBytes, 0, dataBytes.Length);
            encStream.FlushFinalBlock();

            string result = Convert.ToBase64String(stream.ToArray());
            encStream.Close();
            stream.Close();
            return result;
        }
        public string base64edMd5(string data)
        {
            return Convert.ToBase64String(md5(Encoding.UTF8.GetBytes(data)));
        }

        public byte[] md5(byte[] data)
        {
            using MD5CryptoServiceProvider md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }

        private string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "101" => "普通百家乐",
                "102" => "VIP百家乐",
                "103" => "快速百家乐",
                "104" => "咪牌百家乐",
                "201" => "骰宝(HiLo)",
                "301" => "龙虎",
                "401" => "轮盘",
                "501" => "博丁",
                "801" => "牛牛",
                "901" => "炸金花",
                _ => string.Empty,
            };
        }
    }
}
