using Dapper;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Tools _tools;
        private readonly ThirdPartyHelper _thirdPartyHelper;
        private readonly Common _common;
        private readonly IMemoryCache _cache;
        private readonly ServerInfoBLL _serverInfoBll;
        private readonly IReadOnlyDictionary<int, CurrencyOption> _currencyList;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public TpCustInfoService(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IWebHostEnvironment>();
            _tools = provider.GetRequiredService<Tools>();
            _thirdPartyHelper = provider.GetRequiredService<ThirdPartyHelper>();
            _common = provider.GetRequiredService<Common>();
            _cache = provider.GetRequiredService<IMemoryCache>();
            _serverInfoBll = provider.GetRequiredService<ServerInfoBLL>();
            _currencyList = provider.GetRequiredService<IEnumerable<CurrencyOption>>().ToDictionary(d => d.CurrencyId, d => d);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public ApiResult<TpCustInfo> GetTpCustInfo(int custId, int tpId, string langId = "")
        {
            var apiResult = new ApiResult<TpCustInfo>();

            var sTpCustInfoKey = $"TpCustInfo{tpId}_{custId}";
            var tpCustInfo = _cache.Get<TpCustInfo>(sTpCustInfoKey);
            if (tpCustInfo == null)
            {
                // dev not available
                if (CheckTpForbidden(custId, tpId))
                {
                    apiResult.ErrorCode = ErrorCode.Not_allowed_environment;
                    apiResult.Message = "Test environment not supported";
                    return apiResult;
                }

                tpCustInfo = _mainDbClient.QueryFirst<TpCustInfo>("TpCustInfo_Get_V2", new { CustId = custId, TpId = tpId });

                if (tpCustInfo == null)
                {
                    var customer = _mainDbClient.QueryFirst<Customer>("Customer_Get", new { CustId = custId });

                    if (customer == null)
                    {
                        apiResult.ErrorCode = ErrorCode.UserNotExist;
                        return apiResult;
                    }
                    else if (customer.Status == (int)CustStatus.Closed || customer.Status == (int)CustStatus.Suspend)
                    {
                        apiResult.ErrorCode = ErrorCode.UserLocked;
                        return apiResult;
                    }

                    return CreateMember(customer, tpId, true, langId);
                }
                else if (tpCustInfo.Status is (int)CustStatus.Closed or (int)CustStatus.Suspend ||
                         tpCustInfo.CStatus is (int)CustStatus.Closed or (int)CustStatus.Suspend)
                {
                    apiResult.Data = tpCustInfo;
                    apiResult.ErrorCode = ErrorCode.UserLocked;
                    return apiResult;
                }

                if (tpCustInfo is { CustId: > 0 })
                {
                    _cache.Set(sTpCustInfoKey, tpCustInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                    apiResult.Data = tpCustInfo;
                    apiResult.ErrorCode = ErrorCode.Success;
                }
            }
            else
            {
                apiResult.Data = tpCustInfo;
                apiResult.ErrorCode = ErrorCode.Success;
            }

            return apiResult;
        }

        public ApiResult<TpCustInfo> CreateMember(int custId, int tpId, bool isNeedSendRequest)
        {
            var customer = _mainDbClient.QueryFirst<Customer>("Customer_Get", new { CustId = custId });
            return CreateMember(customer, tpId, isNeedSendRequest);
        }

        public void SetTpLastLoginTime(int custId, int tpId)
        {
            _mainDbClient.Execute("TpLastLoginTime_Set", new { TpId = tpId, CustId = custId });
        }

        public void SetTpLastTradingTime(int custId, int tpId)
        {
            _mainDbClient.Execute("TpLastTradingTime_Set", new { TpId = tpId, CustId = custId });
        }

        public CustWalletModel GetCustWallet(int custId)
        {
            return _mainDbClient.QueryFirst<CustWalletModel>("Service_CustWallet_Get", new { CustId = custId });
        }

        /// <summary>
        /// 取得指定第三方客戶資訊清單
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <param name="siteId">平台ID</param>
        /// <returns></returns>
        public IEnumerable<BalanceTpCustInfoModel> GetTpCustInfoList(bool firstRun, int tpId, int siteId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_FirstRun", firstRun);
            parameter.Add("_TpId", tpId);
            parameter.Add("_SiteId", siteId);
            return _reportDbClient.Query<BalanceTpCustInfoModel>("Service_TpCustInfo_Get", parameter);
        }

        public void SetCustGotoGame(int custId, int tpId, int gameType)
        {
            _mainDbClient.Execute("Service_CustGotoGame_Set", new { CustId = custId, TpId = tpId, GameType = gameType });
        }

        public IEnumerable<int> GetCustIdWithoutSabaId(int tpId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TpId", tpId);
            return _reportDbClient.Query<int>("Service_CustWithoutTpCustInfo_Get", parameter);
        }

        public IEnumerable<int> GetCustCurrencySiteIdWithoutSabaId(int tpId, int siteId, int currencyId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TpId", tpId);
            parameter.Add("_SiteId", siteId);
            parameter.Add("_CurrencyId", currencyId);
            return _reportDbClient.Query<int>("Service_CustCurrencySiteWithoutTpCustInfo_Get", parameter);
        }

        public IEnumerable<TpCurrency> GetTpCurrency(int tpId)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TpId", tpId);
            return _reportDbClient.Query<TpCurrency>("Service_TpCurrency_Get", new { TpId = tpId });
        }

        public TpCountryCode GetTpCurrencyCodeByCurrencyId(int tpId, string tpCurrencyId)
        {
            return _mainDbClient.QueryFirst<TpCountryCode>("Service_TpCountryCodeByTpCurrencyId_Get", new { TpId = tpId, TpCurrencyId = tpCurrencyId });
        }

        public TpCustInfo GetTpCustInfoByTpCustId(int tpId, string tpCustId)
        {
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpCustId_Get", new { TpId = tpId, TpCustId = tpCustId });
        }

        private bool CheckTpForbidden(int custId, int tpId)
        {
            // priority custid > tpid
            if (_environment.EnvironmentName != "PROD")
            {
                if (tpId == (int)TpEnum.PT)
                {
                    // allow test custid
                    var _testCustId = new List<long> { };

                    if (_testCustId.Contains(custId))
                    {
                        return false;
                    }
                }

                var _forbiddenTpId = new List<int> { (int)TpEnum.PT, (int)TpEnum.HaoLong };
                if (_forbiddenTpId.Contains(tpId))
                {
                    return true;
                }
            }
            return false;
        }

        private ApiResult<TpCustInfo> CreateMember(Customer customer, int tpId, bool isNeedSendRequest, string langId = "")
        {
            var apiResult = new ApiResult<TpCustInfo>();

            var tpHelper = _thirdPartyHelper.GetThirdPartyHelper(tpId, customer.SiteId);
            var serverInfo = _serverInfoBll.GetServerInfo(customer.SiteId);

            // 特殊幣別TpUserName前綴
            var currencyPrefixCode = "";
            var currencyPrefix = _mainDbClient.QueryFirst<CurrencyPrefix>("Service_CurrencyPrefix_ByCurrencyId_Get", new { _CurrencyId = customer.CurrencyId });
            if (currencyPrefix != null)
            {
                currencyPrefixCode = $"_{currencyPrefix.PrefixCode}";
            }

            var regTpCustInfo = new TpCustInfo
            {
                TpId = tpId,
                CustId = customer.CustId,
                UserName = customer.UserName,
                SiteId = customer.SiteId,
                TpUserName = $"{serverInfo.SiteName}{currencyPrefixCode}_{customer.CustId}",
                IsoCode = GetCurrencyIsoCode(customer.CurrencyId),
                MemberType = customer.MemberType,
                CurrencyId = customer.CurrencyId
            };

            if (_environment.EnvironmentName != "PROD")
            {
                regTpCustInfo.TpUserName = $"{_common.GetEnv()}{serverInfo.SiteName}{currencyPrefixCode}_{customer.CustId}";
            }

            regTpCustInfo.TpPwd = _tools.GetRandomString(7);
            regTpCustInfo.IsTest = customer.IsTest;
            regTpCustInfo.ActStatus = customer.ActStatus;

            var tpCurrency = _mainDbClient.QueryFirst<TpCurrency>("TpCurrency_Get", new { TpId = tpId, customer.CurrencyId });

            if (tpCurrency == null)
            {
                apiResult.ErrorCode = ErrorCode.InvalidCurrency;
                return apiResult;
            }

            regTpCustInfo.TpCurrencyId = tpCurrency.TpCurrencyId;
            regTpCustInfo.ExchangeRate = tpCurrency.ExchangeRate;
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(_thirdPartyHelper.GetThirdPartyBySite(tpId, customer.SiteId).Setting);

            if (!string.IsNullOrEmpty(langId))
            {
                inputs.Add("langId", langId);
            }

            if (inputs == null)
            {
                apiResult.ErrorCode = ErrorCode.SettingNotFound;
                return apiResult;
            }

            if (isNeedSendRequest)
            {
                var createMemberResult = tpHelper.CreateMember(regTpCustInfo, inputs);

                if (createMemberResult.ErrorCode == ErrorCode.Success)
                {
                    _mainDbClient.Execute("TpCustInfo_Ins_V2", regTpCustInfo);
                    regTpCustInfo.Status = (int)CustStatus.Running;
                    regTpCustInfo.CStatus = (int)CustStatus.Running;
                    apiResult.Data = regTpCustInfo;
                    apiResult.ErrorCode = ErrorCode.Success;
                }
                else
                {
                    apiResult.ErrorCode = createMemberResult.ErrorCode;
                    apiResult.Message = createMemberResult.Message;
                    apiResult.RawMessage = createMemberResult.RawMessage;
                    apiResult.RawErrorCode = createMemberResult.RawErrorCode;
                }
            }
            else
            {
                //for sabaId duplicate case
                _mainDbClient.Execute("TpCustInfo_Ins_V2", regTpCustInfo);
                regTpCustInfo.Status = (int)CustStatus.Running;
                regTpCustInfo.CStatus = (int)CustStatus.Running;
                apiResult.Data = regTpCustInfo;
                apiResult.ErrorCode = ErrorCode.Success;
            }
            return apiResult;
        }

        private string GetCurrencyIsoCode(int currencyId)
        {
            return _currencyList.GetValueOrDefault(currencyId).IsoCode;
        }
    }
}