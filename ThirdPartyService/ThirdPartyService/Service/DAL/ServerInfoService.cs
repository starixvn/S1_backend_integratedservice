using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.Library.BonusService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class ServerInfoService
    {

        #region Fields

        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;

        #endregion

        #region Constructors

        public ServerInfoService(IConfiguration configuration, ILogger<MySqlClient> logger)
        {
            _configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 取得指定站台資訊
        /// </summary>
        /// <param name="siteId">站台ID</param>
        /// <returns></returns>
        public ServerInfo GetServerInfo(int siteId)
        {
            using MySqlConnection conn = new MySqlConnection(_configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<ServerInfo>("ServerInfo_Get", new { SiteId = siteId }, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// 取得所有站台資訊
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServerInfo> GetServiceInfoList()
        {
            return _mainDbClient.Query<ServerInfo>("Admin_ServerInfoList_Get");
        }

        #endregion
    }
}
