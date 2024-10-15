using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using static System.Net.WebRequestMethods;

namespace ThirdPartyWager.Service.BLL
{
    public class KMBLL
    {
        private readonly ILogger<KMBLL> _logger;
        private readonly ApiKMBLL _apiKMBLL;
        private readonly WagerKMService _wagerKmService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tp _tp = Tp.KM;

        public KMBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<KMBLL>>();
            _apiKMBLL = provider.GetRequiredService<ApiKMBLL>();
            _wagerKmService = provider.GetRequiredService<WagerKMService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId((int)_tp);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }
                    var kmWagers = await _apiKMBLL.QueryBetHistoryAsync(startTime, endTime, site.SiteId);
                    // 我司将会限制 ** 历史注单 ** 相关的API请求频率 ( 允许商户每五秒请求一次 )
                    Thread.Sleep(6000);

                    // 呼叫API失敗
                    if (kmWagers == null)
                    {
                        return false;
                    }
                    else
                    {
                        if (kmWagers.Count > 0)
                        {
                            foreach (var wager in kmWagers)
                            {
                                wager.beton = wager.beton.ToUniversalTime();
                                wager.betclosedon = wager.betclosedon.ToUniversalTime();
                                wager.betupdatedon = wager.betupdatedon.ToUniversalTime();
                                wager.timestamp = wager.timestamp.ToUniversalTime();
                                var result = await _wagerKmService.InsertWager(wager);
                                if (result > 0)
                                {
                                    _ = _tpUnsettleService.InsertTpUnsettle(Tp.KM, GameType.KM_Poker, wager.ugsbetid);
                                }
                                else
                                {
                                    _logger.LogError($"insert TpWagerKM failed:{JsonConvert.SerializeObject(wager)}");
                                }
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Insert Wager Detail Failed");
                return false;
            }
        }
    }
}