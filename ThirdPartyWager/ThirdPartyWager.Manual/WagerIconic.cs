using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Extensions;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Manual
{
    public class WagerIconic
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiIconicBLL _apiIconicBLL;
        private readonly WagerIconicService _wagerIconicService;
        private readonly ILogger<WagerIconicBLL> _logger;
        private readonly Tp _tpId;
        
        public WagerIconic(ThirdPartyBLL thirdPartyBll, ApiIconicBLL apiIconicBll, WagerIconicService wagerIconicService, ILogger<WagerIconicBLL> logger)
        {
            _thirdPartyBLL = thirdPartyBll;
            _apiIconicBLL = apiIconicBll;
            _wagerIconicService = wagerIconicService;
            _logger = logger;
            _tpId = Tp.Iconic;
        }
        public async Task GetWagerAsync()
        {
            try
            {
                var fileName = "AT.yaml";
                var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                var iconic = deserializer.Deserialize<IconicSetting>(await File.ReadAllTextAsync(fileName));
                
                var setting = _thirdPartyBLL.GetThirdPartySetting((int)_tpId);
                
                foreach (var id in iconic.Ids)
                {
                    _logger.LogInformation($"id:{id}");
                    var dataWager = await _apiIconicBLL.GetBetListById(setting["ApiUrl"], setting[$"token_{iconic.Currency}"], id);
                    
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.error.status;
                        if (errorCode != 0)
                        {
                            _logger.LogError($"THIRD_PARTY_ERROR: {JsonConvert.SerializeObject(dataWager)}");
                        }
                    }
                
                    if (dataWager.totalSize == 0)
                    {
                        _logger.LogInformation($"NO_DATA");
                    }
                    
                    foreach (var bet in dataWager.data)
                    {
                        try
                        {
                            if (bet.gameType != "fish")
                            {
                                continue;
                            }
                            if (bet.status == "playing")
                            {
                                continue;
                            }
                            
                            // 第三方拉到的game會是中文,call sp會有問題,因此設為空反正也不會去修改
                            bet.game = "";
                            _wagerIconicService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            var msg = $"INSERT_WAGER(WAGERID: {bet.id})";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        }
                    }
                }
                _logger.LogInformation($"GetWagersAsync FINISH");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetWagersAsync FATAL_ERROR");
            }
        }
    }

    public class IconicSetting
    {
        public string Currency { get; set; }
        public List<string> Ids { get; set; }
    }
}