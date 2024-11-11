using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.SabaMatch;

public class SabaMatchBLL
{
    private readonly ILogger<SabaOddsBLL> _logger;
    private readonly SabaMatchService _sabaMatchService;
    private readonly HotEventOddsConfigModel _configModel;
    private readonly HotMatchConfigModel _hotMatchConfigModel;
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private const int SuccessErrorCode = 0;

    public SabaMatchBLL(IServiceProvider provider)
    {
        _sabaMatchService = provider.GetRequiredService<SabaMatchService>();
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _hotMatchConfigModel = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HotMatchConfigModel>("HotMatchConfig");
        _common = provider.GetRequiredService<Common>();
        _logger = provider.GetRequiredService<ILogger<SabaOddsBLL>>();
    }

    public async Task<bool> UpdateHotMatch()
    {
        try
        {
            var apiUrl = $"{_hotMatchConfigModel.ApiUrl}/api/saba/event?{CreateQuery()}";

            var response = await _httpHelper.GetClientWithHandler(new HttpClientHandler()).GetAsync(apiUrl);

            var responseResult = await response.Content.ReadAsStringAsync();

            var hotMatchResponse = _common.FromJson<HotMatchResponseModel>(responseResult);

            var matchList = hotMatchResponse.Matches
                .Select(r => new HotMatchInfo
                {
                    MatchId = r.MatchId,
                    LeagueId = r.LeagueId,
                    LeagueName = r.LeagueName,
                    KickOffTime = r.KickOffTime,
                    HomeTeamName = r.TeamInfo.HomeName,
                    AwayTeamName = r.TeamInfo.AwayName,
                    HomeIconUrl = r.TeamInfo.HomeIconUrl,
                    AwayIconUrl = r.TeamInfo.AwayIconUrl,
                    LastUpdateTime = DateTime.Now,
                    LastUpdateUser = "Service",
                    HomeOdds = r.Markets.First().Selections.First(x => x.Key == "h").OddsPrice.DecimalPrice,
                    AwayOdds = r.Markets.First().Selections.First(x => x.Key == "a").OddsPrice.DecimalPrice,
                    Handicap = r.Markets.First().Selections.First(x => x.Key == "h").Point,
                    CreateTime = DateTime.Now
                }).ToList();

            var matchesJson = JsonConvert.SerializeObject(matchList);

            if (!string.IsNullOrEmpty(matchesJson))
            {
                await _sabaMatchService.InsertMatch(new HotMatchRequest { Matches = matchesJson });
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"UpdateHotMatch run fail ExceptionMessage：{ex.Message},Exception：{_common.ToJson(ex)}");
            throw;
        }

        return false;
    }

    private string CreateQuery()
    {
        return $"isLive=false&Bettype=1&from={DateTime.Now.AddHours(-1):yyyy-MM-ddTHH:mm:ss}&to={DateTime.Now:yyyy-MM-ddTHH:mm:ss}&vendorId={_hotMatchConfigModel.VendorId}&vendorMemberId={_hotMatchConfigModel.VendorMemberId}";
    }
}
