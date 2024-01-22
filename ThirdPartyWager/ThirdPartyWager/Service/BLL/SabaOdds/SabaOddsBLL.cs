using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class SabaOddsBLL
    {
        private readonly ILogger<SabaOddsBLL> _logger;
        private readonly SabaOddsService _sabaOddsService;
        private readonly HotEventOddsConfigModel _configModel;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private const int SuccessErrorCode = 0;

        public SabaOddsBLL(IServiceProvider provider)
        {
            _sabaOddsService = provider.GetRequiredService<SabaOddsService>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _configModel = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HotEventOddsConfigModel>("HotEventOddsConfig");
            _common = provider.GetRequiredService<Common>();
            _logger = provider.GetRequiredService<ILogger<SabaOddsBLL>>();
        }

        /// <summary>
        /// 更新 HotEvent 主客隊賠率 + 讓分資訊
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpdateHotEventOddsWithHdp()
        {
            try
            {
                var test = GetSabaOddsMatchId();
                _logger.LogInformation($"GetSabaOddsMatchId={JsonConvert.SerializeObject(test)}");
                foreach (var group in GetSabaOddsMatchId().GroupBy(i => i.GameId.ToString()))//用GameId分類
                {
                    _logger.LogInformation($"group={JsonConvert.SerializeObject(group)}");
                    foreach (var marketType in new List<string>() { "d", "l" })
                    {
                        _logger.LogInformation($"marketType={marketType}");
                        var apiUrl = $"{_configModel.ApiUrl}/api/GetMarkets";
                        var httpContent = CreateFormData(
                            marketType,
                            group.Key,
                            string.Join(",", group.Select(i => i.MatchId)));
                        _logger.LogInformation($"SabaOdds Api={apiUrl}\n Content={JsonConvert.SerializeObject(httpContent)}");
                        var response = await _httpHelper.GetClientWithHandler(new HttpClientHandler()).PostAsync(apiUrl, httpContent);
                        _logger.LogInformation($"SabaOdds Response={JsonConvert.SerializeObject(response)}");
                        var responseResult = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"SabaOdds ResponseResult={JsonConvert.SerializeObject(responseResult)}");
                        var hotEventOddsResponse = _common.FromJson<HotEventOddsResponseModel>(responseResult);

                        if (hotEventOddsResponse.ErrorCode != SuccessErrorCode) return true;
                        if (hotEventOddsResponse.Data?.Markets == null) continue;

                        var oddsList = hotEventOddsResponse.Data.Markets
                            .GroupBy(x => x.EventID)
                            .Select(x => x.MinBy(r => r.Sort)).ToList()
                            .Where(r => r.Selections.Any() && r.MarketStatus == "running")
                            .Select(r => new UpdateOdd
                            {
                                _MatchId = r.EventID,
                                _HomeOdds = r.Selections.First(x => x.Key == "h").Price,  //主隊賠率
                                _AwayOdds = r.Selections.First(x => x.Key == "a").Price,  //客隊賠率
                                _Handicap = r.Selections.First(x => x.Key == "h").Point   //主隊讓分，客隊前端會顯示相對值
                            }).ToList();

                        if (oddsList.Any())
                        {
                            await SetSabaOdds(oddsList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpdateHotEventOddsWithHdp run fail ExceptionMessage：{ex.Message},Exception：{_common.ToJson(ex)}");
                throw;
            }

            return false;
        }

        private FormUrlEncodedContent CreateFormData(string marketType, string gameId, string matchIds)
        {
            var formData = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "vendor_id", _configModel.VendorId },
                { "sport_type", gameId },
                { "market_type", marketType }, //(d和l都要拿回來)
                { "market_version_key", new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString()},
                { "bet_type", "1"},  // 固定1 (我們只拿hdp)
                { "event_ids", matchIds }
            });

            return formData;
        }

        private Task<int> SetSabaOdds(IEnumerable<UpdateOdd> sabaOdds)
        {
            return _sabaOddsService.SetSabaOdds(sabaOdds);
        }

        private IEnumerable<HotEventInfo> GetSabaOddsMatchId()
        {
            return _sabaOddsService.GetSabaOddsMatchId();
        }

    }
}