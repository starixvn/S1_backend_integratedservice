using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore;
using ThirdPartyService.Service.Model;
using Dapper;
using System.Reflection;
using System.Data;

namespace ThirdPartyService.Service.DAL
{
    public class ThirdPartyHelper
    {
        private static ConcurrentDictionary<int, IThirdPartyHelper> ThirdPartyHelperContainer = new ConcurrentDictionary<int, IThirdPartyHelper>();
        private static ConcurrentDictionary<int, ThirdPartyModel> ThirdPartyModelContainer = new ConcurrentDictionary<int, ThirdPartyModel>();
        private readonly IWebHostEnvironment environment;
        private readonly IConfiguration configuration;

        public ThirdPartyHelper(IWebHostEnvironment environment, IConfiguration configuration)
        {
            this.environment = environment;
            this.configuration = configuration;
        }

        public ThirdPartyModel GetThirdPartyModel(int tpId)
        {
            if (!ThirdPartyModelContainer.ContainsKey(tpId))
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    var thirdPartyModel = conn.QueryFirstOrDefault<ThirdPartyModel>("Tp_Get", new { TpId = tpId }, commandType: CommandType.StoredProcedure);
                    if (thirdPartyModel != null)
                        ThirdPartyModelContainer.TryAdd(tpId, thirdPartyModel);
                }
            }

            return ThirdPartyModelContainer.ContainsKey(tpId) ? ThirdPartyModelContainer[tpId] : null;
        }

        public IThirdPartyHelper GetThirdPartyHelper(int tpId)
        {
            if (!ThirdPartyHelperContainer.ContainsKey(tpId))
            {
                var oThirdPartyModel = GetThirdPartyModel(tpId);
                var assembly = Assembly.Load($"ThirdParty.{oThirdPartyModel.TpName}");
                var assemblyType = assembly.GetType($"ThirdParty.{oThirdPartyModel.TpName}.TpHelper");
                //var oThirdPartyHelper = new Lazy<IThirdPartyHelper>(() => (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                //if (oThirdPartyHelper.IsValueCreated)
                ThirdPartyHelperContainer.TryAdd(tpId, (IThirdPartyHelper)Activator.CreateInstance(assemblyType));
                //ThirdPartyHelperContainer[apiType].InsertLog += InsertLog;
                //oThirdPartyModel.ApiName
            }
            return ThirdPartyHelperContainer[tpId];
        }

        public string GetTpLanId(int tpId, string langId)
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.QueryFirstOrDefault<string>("TpLangId_Get", new { TpId = tpId, LangId = langId }, commandType: CommandType.StoredProcedure);
            }
        }
    }
}
