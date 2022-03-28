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
    public class LatestHighlightVideoService
    {
       
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        public LatestHighlightVideoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }
        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            return _mainDbClient.Execute("Service_LatestHighlightVideo_Set_V2", data);
        }
        public int GetLatestHighlightVideo(int siteId, string date, string title)
        {

            return _reportDbClient.QueryFirst<int>("Service_LatestHighlightVideo_Get", new { SiteId = siteId, Date = date, Title = title });
        }
    }
}
