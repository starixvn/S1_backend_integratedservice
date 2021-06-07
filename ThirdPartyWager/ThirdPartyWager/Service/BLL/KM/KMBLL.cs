using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.KM
{
    public class KMBLL
    {
        private readonly ILogger<KMBLL> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiKMBLL _apiKMBLL;
        private readonly WagerKMService _wagerKmService;
        private const int TpId = (int)Tp.KM;
        
        public KMBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _logger = provider.GetRequiredService<ILogger<KMBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiKMBLL = provider.GetRequiredService<ApiKMBLL>();
            _wagerKmService = provider.GetRequiredService<WagerKMService>();
            var config = _thirdPartyBLL.GetThirdPartySetting(TpId);
            _apiKMBLL.KmConfig = config;
        }
        
        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            var kmWagers = await _apiKMBLL.QueryBetHistoryAsync(startTime, endTime);
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
                        if (result != 1)
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