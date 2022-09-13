using MySql.Data.MySqlClient;
using System.Collections.Concurrent;
using ThirdPartyCore;
using ThirdPartyService.Service.Model;
using Dapper;
using System.Reflection;
using System.Data;
using Serilog.Context;

namespace ThirdPartyService.Service.DAL
{
    public class ThirdPartyHelper
    {
        private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new();
        private static ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer = new();
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyHelper> logger;
        private readonly IServiceProvider _provider;

        public ThirdPartyHelper(IServiceProvider provider)
        {
            _provider = provider;
            configuration = provider.GetRequiredService<IConfiguration>();
            logger = provider.GetRequiredService<ILogger<ThirdPartyHelper>>();
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
                var instance = CheckConstructor(assemblyType) ?
                    (IThirdPartyHelper)Activator.CreateInstance(assemblyType, new object[] { _provider }) :
                    (IThirdPartyHelper)Activator.CreateInstance(assemblyType);
                ThirdPartyHelperContainer.TryAdd(tpId, instance);
                ThirdPartyHelperContainer[tpId].InsertLog += InsertLog;
            }
            return ThirdPartyHelperContainer[tpId];
        }
        public void InsertLog(string url, string input, string output)
        {
            LogContext.PushProperty("Tp.Request.URL", url);
            var content = $"Url={url}, Input={input}, Output={output}";
            logger.LogInformation(content);
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
        public IEnumerable<ThirdPartyModel> GetThirdPartyByWalletTypeByCustId(int walletType, int siteId, int custId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<ThirdPartyModel>("Service_TpList_WalletType_ByCustId_Get_V2", new { WalletType = walletType, SiteId = siteId, CustId = custId }, commandType: CommandType.StoredProcedure);
            }
        }
        public BonusWallet CheckBonusWallet(int custid, int tpId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<BonusWallet>("Service_CustBonus_Check", new { CustId = custid, TpId = tpId }, commandType: CommandType.StoredProcedure);
            }
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<TpMaintain>("TpMaintain_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        private bool CheckConstructor(Type type)
        {
            foreach (var constructor in type.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    if (parameter.ParameterType == typeof(IServiceProvider))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
