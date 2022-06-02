using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.SBO
{
    public interface ISBOBLL
    {
        Task<bool> GetWagersAsync(string startTime, string endTime, SBOPortfolio portfolio);
    }

    public class SBOBLL : ISBOBLL
    {
        private readonly IWagerSBOService _wagerSboService;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IApiSBOBLL _apiSboBll;
        private readonly ILogger<SBOBLL> _logger;
        private readonly List<int> _siteList = new List<int>() { 2, 3 };
        public SBOBLL(IWagerSBOService wagerSboService, ThirdPartyBLL thirdPartyBll, IApiSBOBLL apiSboBll)
        {
            _wagerSboService = wagerSboService;
            _thirdPartyBLL = thirdPartyBll;
            _apiSboBll = apiSboBll;
        }
        
        public async Task<bool> GetWagersAsync(string startTime, string endTime, SBOPortfolio portfolio)
        {
            foreach (var siteId in _siteList)
            {
                var dictSetting = _thirdPartyBLL.GetThirdPartyBySite((int)Tp.SBO, siteId);
                var sboSetting = GetSboSetting(dictSetting);
                var sboWagers = await _apiSboBll.GetBetListByModifyDateAsync(startTime, endTime, portfolio, sboSetting);

                if (sboWagers == null) return false;
                    
                if (sboWagers.error.id != 0)
                {
                    _logger.LogError($"get betList by modify date failed: {sboWagers.error.msg}");
                    return false;
                }

                try
                {
                    if (sboWagers.result.Count > 0)
                    {
                        foreach (var wager in sboWagers.result)
                        {
                            var result = await _wagerSboService.InsertWager(wager);
                            if (result != 1)
                            {
                                _logger.LogError($"insert TpWagerSBO failed:{JsonConvert.SerializeObject(wager)}");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e,$"insert TpWagerSBO failed: {e.Message}");
                    return false;
                }
            }

            return true;
        }

        private static SboSetting GetSboSetting(Dictionary<string, string> setting)
        {
            return new SboSetting()
            {
                CompanyKey = setting["CompanyKey"],
                ApiUrl = setting["ApiUrl"],
                ServerId = setting["ServerId"],
                Agent = setting["Agent"]
            };
        }
    }
}