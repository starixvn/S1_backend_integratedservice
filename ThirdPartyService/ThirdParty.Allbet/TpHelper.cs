using System;
using System.Collections.Generic;
using System.Globalization;
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
        private class Result<T>
        {
            public string resultCode { get; set; }
            public string message { get; set; }
            public T data { get; set; }
        }
        
        private class UserResult
        {
            public string player { get; set; }
        }
        
        private class LoginResult
        {
            public string gameLoginUrl { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = inputs.ContainsKey("V2ApiUrl") && !string.IsNullOrEmpty(inputs["V2ApiUrl"]) ? inputs["V2ApiUrl"] : throw new Exception();
            var agentKey = $"V2{tpCustInfo.TpCurrencyId}Agent";
            var agent = inputs.ContainsKey(agentKey) && !string.IsNullOrEmpty(inputs[agentKey]) ? inputs[agentKey] : throw new Exception("Can't get agent.");
            var path = "CheckOrCreate";
            tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}{inputs["V2Suffix"]}";
            var parameters = ToJson(new
            {
                agent,
                player = tpCustInfo.TpUserName
            });
            var header = GetAuthData(inputs, path, parameters, out var contentType);
            
            var result = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<Result<UserResult>>(result);
                if (userResult.resultCode.Equals("OK", StringComparison.OrdinalIgnoreCase) || userResult.resultCode.Equals("PLAYER_EXIST", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawErrorCode = userResult.resultCode;
                    oApiResult.Message = oApiResult.RawMessage = userResult.message;
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string tpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = inputs.ContainsKey("V2ApiUrl") && !string.IsNullOrEmpty(inputs["V2ApiUrl"]) ? inputs["V2ApiUrl"] : throw new Exception();
            var isFun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) && inputs["fun"] == "1";
            var path = isFun ? "LoginTrial" : "Login";
                
            if (!tpCustInfo.TpUserName.EndsWith(inputs["V2Suffix"]))
            {
                tpCustInfo.TpUserName = $"{tpCustInfo.TpUserName}{inputs["V2Suffix"]}";
            }
            
            var parameters = ToJson(new
            {
                player = isFun ? null : tpCustInfo.TpUserName,
                language = tpLangId
            });
            var header = GetAuthData(inputs, path, parameters, out var contentType);

            var result = HttpPostClient($"{apiUrl}/{path}", parameters, contentType, header);
            
            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<Result<LoginResult>>(result);
                if (userResult.resultCode.Equals("OK", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = userResult.data.gameLoginUrl;
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawErrorCode = userResult.resultCode;
                    oApiResult.Message = oApiResult.RawMessage = userResult.message;
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
                "901" => "炸金花/三公",
                _ => string.Empty,
            };
        }
        
        private Dictionary<string, string> GetAuthData(IReadOnlyDictionary<string, string> inputs, string path, string parameters, out string contentType)
        {
            var md5 = Base64Md5(parameters);
            contentType = "application/json";
            var date = DateTime.UtcNow.ToString("dd MMM yyyy HH:mm:ss z", new CultureInfo("en-US"));
            var signString = $"POST\n{md5}\n{contentType}\n{date}\n/{path}";
            var signature = Sign(signString, inputs["V2AllbetKey"]);
            return new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"AB {inputs["V2OperatorId"]}:{signature}" },
                { "Content-MD5", md5 },
                { "Date", date }
            };
        }

        private string Sign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Convert.FromBase64String(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));

            return Convert.ToBase64String(hash);
        }
        
        private string Base64Md5(string data)
        {
            return Convert.ToBase64String(Md5(Encoding.UTF8.GetBytes(data)));
        }
        
        private byte[] Md5(byte[] data)
        {
            using var md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }
    }
}
