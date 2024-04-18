using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class KMBLL
    {
        private readonly ILogger<KMBLL> _logger;
        private readonly ApiKMBLL _apiKMBLL;
        private readonly WagerKMService _wagerKmService;
        private readonly TpUnsettleService _tpUnsettleService;

        public KMBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<KMBLL>>();
            _apiKMBLL = provider.GetRequiredService<ApiKMBLL>();
            _wagerKmService = provider.GetRequiredService<WagerKMService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var kmWagers = await _apiKMBLL.QueryBetHistoryAsync(startTime, endTime);
            // 我司将会限制 ** 历史注单 ** 相关的API请求频率 ( 允许商户每五秒请求一次 )
            Thread.Sleep(6000);

            // 呼叫API失敗
            if (kmWagers == null)
            {
                return false;
            }
            try
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