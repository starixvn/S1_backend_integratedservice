using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerPTBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiPTBLL _apiPTBLL;
        private readonly WagerPTService _wagerPTService;
        private readonly ILogger<WagerPTBLL> _logger;
        private readonly IHostEnvironment _environment;

        private readonly int _tpId = (int)Tp.PT;

        public WagerPTBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiPTBLL = provider.GetRequiredService<ApiPTBLL>();
            _wagerPTService = provider.GetRequiredService<WagerPTService>();
            _logger = provider.GetRequiredService<ILogger<WagerPTBLL>>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startUnix, string endUnix, string currency, int siteId, int gmt)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);
                string apiUrl = setting["ApiUrl"];
                string pass = setting[$"Pass_{currency}"];
                string entityKey = setting[$"XEntityKey_{currency}"];

                Dictionary<string, string> certificate = new Dictionary<string, string>();
                certificate.Add("path", $@"{_environment.ContentRootPath}\Storage\PT\{currency}.p12");
                certificate.Add("password", pass);
                certificate.Add("currency", currency);
                certificate.Add("site", siteId.ToString());

                int perPage = 500;
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await _apiPTBLL.GetBetListByTime(apiUrl, entityKey, certificate, startUnix, endUnix, page, perPage, guid);

                    // error condition
                    if (dataWager.error != null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return false;
                    }

                    if (dataWager.pagination.totalCount == 0)
                    {
                        msg = "NO_DATA";
                        _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                        return true;
                    }

                    totalPage = dataWager.pagination.totalPages;
                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            // enter/exit game no need to insert
                            if (bet.EXITGAME == 1)
                            {
                                continue;
                            }

                            bet.gameUnix = new DateTimeOffset(bet.GAMEDATE, new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                            _wagerPTService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.GAMECODE} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startUnix} ~ {endUnix}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
