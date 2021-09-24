using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.BLL
{
    public class ServerInfoBLL
    {
        #region Fields

        private readonly ServerInfoService _serverInfoService;
        private ConcurrentDictionary<int, ServerInfo> serverInfoCache = new ConcurrentDictionary<int, ServerInfo>();

        #endregion

        #region Construstors

        public ServerInfoBLL(ServerInfoService serverInfoService)
        {
            _serverInfoService = serverInfoService;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 取得指定站台資訊
        /// </summary>
        /// <param name="siteId">站台ID</param>
        /// <returns></returns>
        public ServerInfo GetServerInfo(int siteId)
        {
            if(serverInfoCache.ContainsKey(siteId))
            {
                return serverInfoCache[siteId];
            }
            else
            {
                var serverInfo = _serverInfoService.GetServerInfo(siteId);
                serverInfoCache.TryAdd(siteId, serverInfo);
                return serverInfo;
            }
        }

        /// <summary>
        /// 取得所有站台資訊清單
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServerInfo> GetServerInfoList()
        {
            return _serverInfoService.GetServiceInfoList();
        }

        #endregion
    }
}
