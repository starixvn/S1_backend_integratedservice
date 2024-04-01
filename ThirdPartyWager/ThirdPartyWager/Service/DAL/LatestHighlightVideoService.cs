using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class LatestHighlightVideoService
    {
        private readonly MySqlClient _mainDbClient;

        public LatestHighlightVideoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            return _mainDbClient.Execute("Service_LatestHighlightVideo_Set_V2", data);
        }

        public int GetLatestHighlightVideo(int siteId, string date, string title)
        {
            return _mainDbClient.QueryFirst<int>("Service_LatestHighlightVideo_Get", new { SiteId = siteId, Date = date, Title = title });
        }
    }
}
