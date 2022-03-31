using System;
using System.Linq;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class BetLimitService : IBetLimitProvider
    {
        private readonly MySqlClient _reportDbClient;
        public BetLimitService(IServiceProvider provider)
        {
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public CustBetLimit GetCustBetLimit(int custId)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    _CustId = custId,
                });

                return _reportDbClient.QueryFirst<CustBetLimit>("Service_CustBetLimitByCustId_Get", p);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}