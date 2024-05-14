using System;
using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using System.Text.Json;
using Serilog.Context;

using ThirdPartyCore;
using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class ThirdPartyHelper
    {
        private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new();
        private static ConcurrentDictionary<string, ThirdPartyModel> ThirdPartyModelContainer = new();
        private readonly ILogger<ThirdPartyHelper> _logger;
        private readonly IServiceProvider _provider;
        private readonly MySqlClient _mainDbClient;

        public ThirdPartyHelper(IServiceProvider provider)
        {
            _provider = provider;
            _logger = provider.GetRequiredService<ILogger<ThirdPartyHelper>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            var sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = _mainDbClient.QueryFirst<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId });
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }
            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
        }

        public ThirdPartyModel GetThirdPartySetting(int tpId)
        {
            return _mainDbClient.QueryFirst<ThirdPartyModel>("Tp_Get", new { TpId = tpId });
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

        private void InsertLog(string url, string input, string output)
        {
            LogContext.PushProperty("Tp.Request.URL", url);
            var content = $"Url={url}, Input={input}, Output={output}";
            _logger.LogInformation(content);
        }

        public string GetTpLanId(int tpId, string langId)
        {
            return _mainDbClient.QueryFirst<string>("TpLangId_Get", new { TpId = tpId, LangId = langId });
        }

        public IEnumerable<ThirdPartyModel> GetThirdPartyByWalletType(int walletType, int siteId)
        {
            return _mainDbClient.Query<ThirdPartyModel>("Service_TpList_WalletType_Get", new { WalletType = walletType, SiteId = siteId });
        }

        public IEnumerable<ThirdPartyModel> GetThirdPartyByWalletTypeByCustId(int walletType, int siteId, int custId)
        {
            return _mainDbClient.Query<ThirdPartyModel>("Service_TpList_WalletType_ByCustId_Get_V2", new { WalletType = walletType, SiteId = siteId, CustId = custId });
        }

        public BonusWallet CheckBonusWallet(int custid, int tpId)
        {
            return _mainDbClient.QueryFirst<BonusWallet>("Service_CustBonus_Check", new { CustId = custid, TpId = tpId });
        }

        public IEnumerable<TpMaintain> GetTpMaintainList(int siteId)
        {
            return _mainDbClient.Query<TpMaintain>("TpMaintain_Get", new { SiteId = siteId });
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
