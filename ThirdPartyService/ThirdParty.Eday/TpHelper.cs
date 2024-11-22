using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Eday
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        private const string ContentType = "application/json";
        private readonly Dictionary<int, EdayConfig> _siteConfigDic;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<EdayConfig>(TpEnum.Eday.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var path = "/apilot/CreatePlayer";
            
            var parameters = ToJson(new
            {
                player = tpCustInfo.TpUserName,
                status = 1 
            });
            
            var header = GetAuthData(inputs, path, parameters,siteConfig);
            var result = HttpPostClient($"{apiUrl}/{path}", parameters, ContentType, header);
            
            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                //CreateMember無 Data
                var userResult = FromJson<EdayResult<EdayLoginResponse>>(result);
                if (userResult.resultCode.Equals("0", StringComparison.OrdinalIgnoreCase))
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

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId,
            Dictionary<string, string> inputs)
        {
            var uuid = Guid.NewGuid();
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var path = "/apilot/OpenGameSession";
            
            var parameters = ToJson(new
            { 
                txId = uuid,
                player = tpCustInfo.TpUserName,
                lang = TpLangId,
                force = true
            });
            
            var header = GetAuthData(inputs, path, parameters,siteConfig);
            var result = HttpPostClient($"{apiUrl}/{path}", parameters, ContentType, header);
            
            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var userResult = FromJson<EdayResult<EdayLoginResponse>>(result);
                if (userResult.resultCode.Equals("0", StringComparison.OrdinalIgnoreCase))
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.RawErrorCode = userResult.resultCode;
                    oApiResult.Message = oApiResult.RawMessage = userResult.data.gameUrl;
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
        
        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var path = "/apilot/Logout";
            
            var parameters = ToJson(new
            {
                player = tpCustInfo.TpUserName,
            });
            
            var header = GetAuthData(inputs, path, parameters,siteConfig);
            var result = HttpPostClient($"{apiUrl}/{path}", parameters, ContentType, header);
            
            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                //LogOut無 給LoginResponse暫時代替
                var userResult = FromJson<EdayResult<EdayLoginResponse>>(result);
                if (userResult.resultCode.Equals("0", StringComparison.OrdinalIgnoreCase))
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
        
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            //拿Bettrans，Eday無注單明細API，注單明細為Json不是Url，若需要再改
            var spName = "Service_BettransLT_Eday_Detail_Get";
            var db = DataBase.Main;

            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            
            var wagerDetail = TpBetRecordProvider.DbQueryFirst(spName, new { getBetDetail.TransId }, db);
            var detail = ToJson(wagerDetail);
            var transId =
                $"{wagerDetail.GetValueOrDefault("roundCode")}_{wagerDetail.GetValueOrDefault("player")}_{wagerDetail.GetValueOrDefault("billId")}";
            
            return new LotteryDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = transId,
                Detail = detail,
                TpCurrency = tpCurrency,
                RefTransId = getBetDetail.TransId,
                TpUserName = wagerDetail.GetValueOrDefault("player")?.ToString(),
                UTCBetTime = DateTime.Parse(wagerDetail.GetValueOrDefault("UTCBetTime")?.ToString()),
                UTCSettledTime = DateTime.Parse(wagerDetail.GetValueOrDefault("UTCSettleTime")?.ToString()),
                Stake = (decimal)wagerDetail.GetValueOrDefault("amount"),
                WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("winTotal") 
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType,
            TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_Eday_Get", new { id = betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameCode"),
                Game = tpGameType.TypeName,
                RefNo = detail.GetStringValue("roundCode"),
                Title = tpGameType.TypeName,
                MatchID = betRecordInfo.TransId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }
        
        #region UnUse
        
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }
        
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs,
            decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
        #region Private
        
        private Dictionary<string, string> GetAuthData(IReadOnlyDictionary<string, string> inputs, string path, string parameters,EdayConfig siteConfig)
        {
            var md5 = Base64Md5(parameters);
            var contentType = "application/json";
            var signString = $"POST\n{md5}\n{contentType}\n{path}";
            var signature = Sign(signString, siteConfig.ApiKey);
            return new Dictionary<string, string>
            {
                { "Accept", "application/json" },
                { "Authorization", $"ED {siteConfig.Operator}:{signature}" },
                { "Content-MD5", md5 }
            };
        }
        
        private string Sign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));
            
            return Convert.ToBase64String(hash);
        }
        private string Base64Md5(string data)
        {
            return Convert.ToBase64String(Md5(Encoding.ASCII.GetBytes(data)));
        }
        private byte[] Md5(byte[] data)
        {
            using var md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }
        
        #endregion
    }
}