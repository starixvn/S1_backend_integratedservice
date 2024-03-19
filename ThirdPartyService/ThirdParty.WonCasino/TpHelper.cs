using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.WonCasino
{
    public class TpHelper : ThirdPartyHelperBase , IThirdPartyParticular
    {
        private readonly Dictionary<int, WonCasinoConfig> _siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<WonCasinoConfig>(TpEnum.WonCasino.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //WonCasino Login 及 CreateMember都是同一隻API。
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            var fun = GetValueOrDefault(inputs, "fun", "0");
            
            try
            {
                //WonCasino Login 及 CreateMember都是同一隻API。
                var req = new SortedDictionary<string, object>
                {
                    {"appid", currencyConfig.AppId},
                    {"username", tpCustInfo.TpUserName},
                    {"nickname", tpCustInfo.UserName},
                    {"iscreate", "1"},
                    {"clienttype", (int)appType},
                    {"language", TpLangId},
                    {"currency", currencyConfig.CurrencyCode},
                };

                if (fun == "1")
                {
                    req.Add("ist","1");
                }

                var sign = GetSign(req, currencyConfig.SecretKey).ToLower();
                req.Add("sign",sign);

                
                var queryString =  string.Join("&", req.OrderBy(p => p.Key)
                    .Select(p => $"{Uri.EscapeDataString(p.Key).ToLower()}={Uri.EscapeDataString(p.Value.ToString())}"));
                
                var result = HttpPostWithoutParameters(apiUrl + "/api/login?" + queryString,null,30);
                
                if (!IsJSON(result))
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Fail,
                        RawErrorCode = "1",
                        RawMessage = "No Result. Response is not json."
                    };
                }
                
                var jsonResponse = JsonConvert.DeserializeObject<WonLoginResponse>(result);

                if (jsonResponse == null)
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Fail,
                        RawErrorCode = "1",
                        RawMessage = "No Result.Response is null."
                    };
                }

                if (jsonResponse.result == 0 || jsonResponse.desc == "succeed")
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Success,
                        Message = jsonResponse.openurl
                    };
                }

                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result.Response is null."
                };;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteConfig = _siteConfigDic[tpCustInfo.SiteId];
            var apiUrl = siteConfig.ApiUrl;
            var currencyConfig = siteConfig.CurrencyList.FirstOrDefault(x => x.CurrencyCode == tpCustInfo.TpCurrencyId);
            
            try
            {
                var req = new SortedDictionary<string, object>
                {
                    {"appid", currencyConfig.AppId},
                    {"username", tpCustInfo.TpUserName},
                };

                var sign = GetSign(req, currencyConfig.SecretKey).ToLower();
                req.Add("sign",sign);

                
                var queryString =  string.Join("&", req.OrderBy(p => p.Key)
                    .Select(p => $"{Uri.EscapeDataString(p.Key).ToLower()}={Uri.EscapeDataString(p.Value.ToString())}"));
                
                var result = HttpPostWithoutParameters(apiUrl + "/api/kickout?" + queryString,null,30);
                
                if (!IsJSON(result))
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Fail,
                        RawErrorCode = "1",
                        RawMessage = "No Result. Response is not json."
                    };
                }
                
                var jsonResponse = JsonConvert.DeserializeObject<WonLoginResponse>(result);

                if (jsonResponse == null)
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Fail,
                        RawErrorCode = "1",
                        RawMessage = "No Result.Response is null."
                    };
                }

                if (jsonResponse.result == 0 || jsonResponse.desc == "succeed")
                {
                    return new ApiResult()
                    {
                        ErrorCode = ErrorCode.Success,
                        Message = jsonResponse.desc
                    };
                }

                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result.Response is null."
                };;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        #region private

        private string GetSign(SortedDictionary<string, object> fields, string secretKey)
        {
            var sortedData = fields.OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{kvp.Key.ToLower()}={kvp.Value}")
                .ToList();
            
            var rawData = string.Join("&", sortedData) + "&key=" + secretKey;
            
            return ToMD5(rawData);
        }
        
        private string GetValueOrDefault(IReadOnlyDictionary<string, string> inputs, string key, string defaultValue = null, bool throwException = false)
        {
            if (!inputs.ContainsKey(key) || string.IsNullOrEmpty(inputs[key]))
            {
                if (throwException)
                    throw new Exception($"Missing or empty input for key: {key}");
                return defaultValue;
            }
            return inputs[key];
        }

        private string ConvertLanguage(string languageCode)
        {
            return languageCode switch
            {
                "1" => "1",
                "2" => "2",
                "5" => "5",
                "10" => "10",
                "11" => "11",
                _ => "2"
            };
        }
        
        #endregion
        
        #region Transfer
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}