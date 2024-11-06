using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class SabaGameDetailBLL
    {
        private const Tp TpId = Tp.SBSW;
        private readonly string _tpName = Tp.SBSW.ToString();
        private readonly Log2File _log2File;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly Dictionary<int, SabaGameDetailConfigModel> _siteConfigDic;
        private readonly HttpHelper _httpHelper;
        private readonly SabaGameDetailService _sabaGameDetailService;

        public SabaGameDetailBLL(IServiceProvider provider)
        {
            _log2File = provider.GetRequiredService<Log2File>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SabaGameDetailConfigModel>(TpId.ToString());
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _sabaGameDetailService = provider.GetRequiredService<SabaGameDetailService>();
        }
        
        public async Task<bool> GetSabaGameDetail()
        {
            var siteList = _thirdPartyBll.GetTpListByTpId((int)TpId);

            foreach (var site in siteList)
            {
                if (site.TicketKey == 0)
                {
                    continue;
                }
                
                // 分Site取得TpList
                var config = _siteConfigDic[site.SiteId];
                
                var apiUrl = $"{config.ApiUrl}/GetGameDetail";

                var hotEventInfos = _sabaGameDetailService.GetHotEventInfo(site.SiteId);
                if (!hotEventInfos.Any())
                {
                   continue;
                }
                
                var matchIds = hotEventInfos.Count > 1 ? string.Join("," , hotEventInfos.Select(x=>x.MatchId)) : hotEventInfos.First().MatchId.ToString();
                var parameters = new Dictionary<string, string>
                {
                    {"vendor_id", config.VendorId},
                    {"match_ids", matchIds}
                };
                
                var p = string.Join("&", parameters.Select(kp => $"{kp.Key}={kp.Value}"));
                var request = await DoHttpClientAsync(apiUrl, p);
                var result = JsonConvert.DeserializeObject<SabaGameDetailResponse>(request);

                if (result.ErrorCode == 0)
                {
                    foreach (var data in result.Data)
                    {
                        /// 目前只需要比分 , 其他需要在加
                        if (hotEventInfos.Select(x => x.MatchId).Contains(data.MatchId))
                        {
                            var homeScore = data.HomeScore;
                            var awayScore = data.AwayScore;
                            _sabaGameDetailService.SetHotEventMatchIdScore(homeScore, awayScore, data.MatchId);
                        }
                    }
                }
            }

            return false;
        }
        
        private async Task<string> DoHttpClientAsync(string url, string para)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(para, Encoding.UTF8, "application/x-www-form-urlencoded");
            _log2File.InsertRequestLog(_tpName, url, $"{para}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

            var response = await _httpHelper.GetClient().SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsStringAsync().Result;
                _log2File.InsertResponseLog(_tpName, url, para, result);
                
                return result;
            }
            catch (HttpRequestException hrEx)
            {
                _log2File.InsertResponseLog(_tpName, url, para, hrEx.Message);
                return null;
            }
        }
        
    }
}