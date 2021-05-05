using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyService.Service.Model;
using Dapper;
using System.Reflection;
using System.Data;
using Microsoft.Extensions.Logging;
using ThirdPartyService.Service.Library;
using System.Collections.Generic;
using Serilog.Context;

namespace ThirdPartyService.Service.DAL
{
    public class ThirdPartyHelper
    {
        private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new ConcurrentDictionary<int, IThirdPartyHelper>();
        private static ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartyModel>();
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyHelper> logger;
        private readonly Log2File _log2File;

        public ThirdPartyHelper(ILogger<ThirdPartyHelper> logger, IWebHostEnvironment environment, IConfiguration configuration, Log2File log2File)
        {
            this.environment = environment;
            this.configuration = configuration;
            this.logger = logger;
            _log2File = log2File;
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            string sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                var thirdPartyModel = conn.QueryFirstOrDefault<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId }, commandType: CommandType.StoredProcedure);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
        }

        public ThirdPartyModel GetThirdPartySetting(int tpId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ThirdPartyModel>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
        }

        public IThirdPartyHelper GetThirdPartyHelper(int tpId, int siteId)
        {
            if (!ThirdPartyHelperContainer.ContainsKey(tpId))
            {
                var oThirdPartyModel = GetThirdPartyBySite(tpId, siteId);
                LogContext.PushProperty("TP", oThirdPartyModel.TpName);
                var assembly = Assembly.Load($"ThirdParty.{oThirdPartyModel.TpName}");
                var assemblyType = assembly.GetType($"ThirdParty.{oThirdPartyModel.TpName}.TpHelper");
                //var oThirdPartyHelper = new Lazy<IThirdPartyHelper>(() => (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                //if (oThirdPartyHelper.IsValueCreated)
                ThirdPartyHelperContainer.TryAdd(tpId, (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                ThirdPartyHelperContainer[tpId].InsertLog += InsertLog;
                //oThirdPartyModel.ApiName
            }
            return ThirdPartyHelperContainer[tpId];
        }
        public void InsertLog(string url, string input, string output)
        {
            var content = $"Url={url}, Input={input}, Output={output}";
            logger.LogInformation(content);
            var action = url.Split('/').LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
            _log2File.Info(content, action);
        }

        public string GetTpLanId(int tpId, string langId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<string>("TpLangId_Get", new { TpId = tpId, LangId = langId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<ThirdPartyModel> GetThirdPartyByWalletType(int walletType, int siteId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<ThirdPartyModel>("Service_TpList_WalletType_Get", new { WalletType = walletType, SiteId = siteId }, commandType: CommandType.StoredProcedure);
            }
        }
        public BonusWallet CheckBonusWallet(int custid, int tpId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<BonusWallet>("Service_CustBonus_Check", new { CustId = custid, TpId = tpId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
