using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using Dapper;
using System.Data;
using ThirdPartyCore.Model.enums;
using Microsoft.AspNetCore.Hosting;
using ThirdPartyCore;
using ThirdPartyService.Service.Library;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment environment;
        private readonly Tools tools;
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly Common common;
        private readonly IMemoryCache cache;
        private readonly ServerInfoBLL serverInfoBLL;

        public TpCustInfoService(IWebHostEnvironment environment, IConfiguration configuration, IMemoryCache cache, Tools tools, ThirdPartyHelper thirdPartyHelper, Common common, ServerInfoBLL serverInfoBLL)
        {
            this.configuration = configuration;
            this.environment = environment;
            this.tools = tools;
            this.thirdPartyHelper = thirdPartyHelper;
            this.common = common;
            this.cache = cache;
            this.serverInfoBLL = serverInfoBLL;
        }
        public ApiResult<TpCustInfo> GetTpCustInfo(int custId, int tpId)
        {
            var apiResult = new ApiResult<TpCustInfo>();
            TpCustInfo tpCustInfo = null;

            string sTpCustInfoKey = $"TpCustInfo{tpId}_{custId}";
            tpCustInfo = cache.Get<TpCustInfo>(sTpCustInfoKey);
            if (tpCustInfo == null)
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    tpCustInfo = conn.QueryFirstOrDefault<TpCustInfo>("TpCustInfo_Get", new { CustId = custId, TpId = tpId }, commandType: CommandType.StoredProcedure);

                    if (tpCustInfo == null)
                    {
                        var customer = conn.QueryFirstOrDefault<Customer>("Customer_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);

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

                        IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpId, customer.SiteId);
                        var serverInfo = serverInfoBLL.GetServerInfo(customer.SiteId);

                        var regTpCustInfo = new TpCustInfo();
                        regTpCustInfo.TpId = tpId;
                        regTpCustInfo.CustId = customer.CustId;
                        regTpCustInfo.UserName = customer.UserName;
                        regTpCustInfo.SiteId = customer.SiteId;
                        regTpCustInfo.TpUserName = $"{serverInfo.SiteName}_{customer.CustId}";

                        if (environment.EnvironmentName != "Production")
                        {
                            regTpCustInfo.TpUserName = $"{common.GetEnv()}{serverInfo.SiteName}_{customer.CustId}";
                        }

                        regTpCustInfo.TpPwd = tools.GetRandomString(7);
                        regTpCustInfo.IsTest = customer.IsTest;
                        regTpCustInfo.ActStatus = customer.ActStatus;

                        if (customer.IsTest)
                        {
                            regTpCustInfo.TpUserName = $"{regTpCustInfo.TpUserName}_test";
                        }

                        var tpCurrency = conn.QueryFirstOrDefault<TpCurrency>("TpCurrency_Get", new { TpId = tpId, customer.CurrencyId }, commandType: CommandType.StoredProcedure);

                        if (tpCurrency == null)
                        {
                            apiResult.ErrorCode = ErrorCode.InvalidCurrency;
                            return apiResult;
                        }

                        regTpCustInfo.TpCurrencyId = tpCurrency.TpCurrencyId;
                        regTpCustInfo.ExchangeRate = tpCurrency.ExchangeRate;
                        Dictionary<string, string> inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyBySite(tpId, customer.SiteId).Setting);

                        if (inputs == null)
                        {
                            apiResult.ErrorCode = ErrorCode.SettingNotFound;
                            return apiResult;
                        }

                        var createMemberResult = tpHelper.CreateMember(regTpCustInfo, inputs);

                        if (createMemberResult.ErrorCode == ErrorCode.Success)
                        {
                            conn.Execute("TpCustInfo_Ins", regTpCustInfo, commandType: CommandType.StoredProcedure);
                            regTpCustInfo.Status = (int)CustStatus.Running;
                            regTpCustInfo.CStatus = (int)CustStatus.Running;
                            tpCustInfo = regTpCustInfo;
                            apiResult.Data = tpCustInfo;
                            apiResult.ErrorCode = ErrorCode.Success;
                        }
                        else
                        {
                            apiResult.ErrorCode = createMemberResult.ErrorCode;
                            apiResult.RawMessage = createMemberResult.RawMessage;
                            apiResult.RawErrorCode = createMemberResult.RawErrorCode;
                            return apiResult;
                        }
                    }
                    else if (tpCustInfo.Status == (int)CustStatus.Closed ||
                        tpCustInfo.Status == (int)CustStatus.Suspend ||
                        tpCustInfo.CStatus == (int)CustStatus.Closed ||
                        tpCustInfo.CStatus == (int)CustStatus.Suspend)
                    {
                        apiResult.ErrorCode = ErrorCode.UserLocked;
                        return apiResult;
                    }

                    if (tpCustInfo != null && tpCustInfo.CustId > 0)
                    {
                        cache.Set(sTpCustInfoKey, tpCustInfo, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                        apiResult.Data = tpCustInfo;
                        apiResult.ErrorCode = ErrorCode.Success;
                    }
                }
            }
            else
            {
                apiResult.Data = tpCustInfo;
                apiResult.ErrorCode = ErrorCode.Success;
            }

            return apiResult;
        }
        public void SetTpLastLoginTime(int custId, int tpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("TpLastLoginTime_Set", new { TpId = tpId, CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public void SetTpLastTradingTime(int custId, int tpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Execute("TpLastTradingTime_Set", new { TpId = tpId, CustId = custId }, commandType: CommandType.StoredProcedure);
        }
    }
}
