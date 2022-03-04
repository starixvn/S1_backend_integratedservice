using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class CommonService
    {
        private readonly IEnumerable<ServerInfo> _serverInfos;

        private readonly MySqlClient _reportDbClient;

        public CommonService(IServiceProvider provider)
        {
            _serverInfos = provider.GetRequiredService<IEnumerable<ServerInfo>>();
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        public IEnumerable<ServerInfo> GetSiteList()
        {
            return _serverInfos;
        }

        public IEnumerable<TpCurrency> GetTpCurrencyList(int tpId)
        {
            return _reportDbClient.Query<TpCurrency>("Service_TpCurrency_Get", new { TpId = tpId });
        }
    }
}

