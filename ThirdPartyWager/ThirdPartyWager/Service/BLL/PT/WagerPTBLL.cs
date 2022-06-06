using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
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

        public WagerPTBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiPTBLL = provider.GetRequiredService<ApiPTBLL>();
            _wagerPTService = provider.GetRequiredService<WagerPTService>();
            _logger = provider.GetRequiredService<ILogger<WagerPTBLL>>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string startUnix, string endUnix, string currency, int siteId, int gmt, WagerType wager = WagerType.Wager ,int gmtMinute = 0 )
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);
                var apiUrl = setting["ApiUrl"];
                var pass = setting[$"Pass_{currency}"];
                var entityKey = setting[$"XEntityKey_{currency}"];
                var certificate = new Dictionary<string, string>
                {
                    { "path", Path.Combine(_environment.ContentRootPath, "Storage", "PT", $"{currency}.p12") },
                    { "password", pass },
                    { "currency", currency },
                    { "site", siteId.ToString() }
                };

                var perPage = 500;
                var page = 1;
                var totalPage = 1;
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

                            bet.gameUnix = new DateTimeOffset(bet.GAMEDATE, new TimeSpan(gmt, gmtMinute, 0)).ToUnixTimeSeconds();
                            
                            switch (wager)
                            {
                                case WagerType.Wager:
                                    _wagerPTService.InsertWager(bet);
                                    break;
                                
                                case WagerType.WagerReSettle:
                                    
                                    var tpCustInfo = _thirdPartyBLL.GetTpCustInfoWithCache(tpId, bet.PLAYERNAME);
                                    if (tpCustInfo == null)
                                    {
                                        break;
                                    }
                                    
                                    var settleData = new BetRecordInfo
                                    {
                                        TpId = (int) Tp.PT,
                                        TransId = bet.GAMECODE,
                                        GameType = string.Equals(bet.GAMETYPE, "Live Games", StringComparison.OrdinalIgnoreCase) ? GameType.PT_LiveCasino : GameType.PT_RNG,
                                        TpUserName = bet.PLAYERNAME,
                                        SettledTime = bet.GAMEDATE,
                                        TransTime = bet.GAMEDATE,
                                        Stake = bet.BET,
                                        ValidBet = bet.BET,
                                        Revenue = null,
                                        ProgressiveBet = bet.PROGRESSIVEBET,
                                        ProgressiveWin = bet.PROGRESSIVEWIN,
                                        WinlostAmount = (bet.WIN + bet.PROGRESSIVEWIN) - bet.BET,
                                        SettlementType = SettlementType.SettledForValid,
                                        OriginalState = null,
                                        TicketKey = 0,
                                        BonusId = 0
                                    };

                                    if (settleData.GameType == GameType.PT_RNG)
                                    {
                                        settleData.SettlementType = _thirdPartyBLL.GetGameCasinoSettlementType(tpId, bet.SHORTNAME);
                                    }
                                    
                                    #region TicketStatus

                                    if (settleData.WinlostAmount > 0)
                                    {
                                        settleData.TicketStatus = settleData.Stake != 0 ? TicketStatus.Win : TicketStatus.FreeBetWin;
                                    }
                                    else if (settleData.WinlostAmount == 0)
                                    {
                                        settleData.TicketStatus = TicketStatus.Draw;
                                    }
                                    else
                                    {
                                        settleData.TicketStatus = TicketStatus.Lost;
                                    }

                                    #endregion
                                    
                                    _wagerPTService.InsertTpBetRecord14(settleData , tpCustInfo);
                                    break;
                                
                                default:
                                    _wagerPTService.InsertWager(bet);
                                    break;
                            }
                            
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
