using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class CustBetLimitService : ICustBetLimitService
    {
        private readonly MySqlClient _sqlClient;

        public CustBetLimitService(IServiceProvider provider)
        {
            _sqlClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public CustBetLimit GetCustBetLimit(int custId)
        {
            var p = new DynamicParameters(new { _CustId = custId });
            var result = _sqlClient.QueryFirst<CustBetLimitData>("Service_CustBetLimitByCustId_Get", p);
            if (result == null || result.BetSetting == null || result.BetSetting == "null")
                return null;
            
            var custBetLimit = new CustBetLimit()
            {
                CustId = result.CustId,
                SiteId = result.SiteId,
                BetSettings = JsonConvert.DeserializeObject<List<BetSetting>>(result.BetSetting)
            };
            return custBetLimit;
        }
    }
}