using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.BLL
{
    public class ServerInfoBLL
    {
        private readonly ServerInfoService serverInfoService;
        private ConcurrentDictionary<int, ServerInfo> serverInfoCache = new ConcurrentDictionary<int, ServerInfo>();
        public ServerInfoBLL(ServerInfoService serverInfoService)
        {
            this.serverInfoService = serverInfoService;
        }

        public ServerInfo GetServerInfo(int siteId)
        {
            if(serverInfoCache.ContainsKey(siteId))
            {
                return serverInfoCache[siteId];
            }
            else
            {
                var serverInfo = serverInfoService.GetServerInfo(siteId);
                serverInfoCache.TryAdd(siteId, serverInfo);
                return serverInfo;
            }
        }
    }
}
