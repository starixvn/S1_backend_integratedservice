using System.Text.RegularExpressions;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSkywindBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerSkywindBLL> _logger;
        private readonly ApiSkywindBLL _apiSkywindBLL;
        private const int TpId = (int)Tp.Skywind;
        private Dictionary<string, string> liveaCasino;
        private Dictionary<string, string> fishing;

        public WagerSkywindBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerSkywindBLL>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSkywindBLL = provider.GetRequiredService<ApiSkywindBLL>();
            liveaCasino = new Dictionary<string, string>();
            fishing = new Dictionary<string, string>();
            liveaCasino.Add("sw_ro_a01bac", "sw_ro_a01bac");
            liveaCasino.Add("sw_ro_a01bac_nc", "sw_ro_a01bac_nc");
            liveaCasino.Add("sw_ro_a04bac", "sw_ro_a04bac");
            liveaCasino.Add("sw_ro_a04bac_nc", "sw_ro_a04bac_nc");
            liveaCasino.Add("sw_ro_a07bac", "sw_ro_a07bac");
            liveaCasino.Add("sw_ro_a07bac_nc", "sw_ro_a07bac_nc");
            liveaCasino.Add("sw_ro_a01ro", "sw_ro_a01ro");
            liveaCasino.Add("sw_ro_a03ro", "sw_ro_a03ro");
            liveaCasino.Add("sw_live_erol_atom", "sw_live_erol_atom");
            fishing.Add("sw_or", "sw_or");
            fishing.Add("sw_fj", "sw_fj");
            fishing.Add("sw_cashdaha", "sw_cashdaha");
        }

        public async Task<bool> GetWagersAsync(int tpId, string apiUrl, string secretKey, string username, string password, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            try
            {
                var dataWager = await _apiSkywindBLL.GetBetListByTime(apiUrl, secretKey, username, password, startTime, endTime);

                foreach (var bet in dataWager)
                {
                    try
                    {
                        var betSkywind = new BetSkywind()
                        {
                            finished = bet.finished ? 1 : 0,
                            revenue = bet.revenue,
                            currency = bet.currency,
                            debit = bet.debit.HasValue ? bet.debit.Value : 0,
                            credit = bet.credit.HasValue ? bet.credit.Value : 0,
                            roundId = bet.roundId,
                            brandId = bet.brandId,
                            playerCode = bet.playerCode,
                            gameCode = bet.gameCode,
                            device = bet.device,
                            win = bet.win,
                            bet = bet.bet,
                            balanceAfter = bet.balanceAfter.HasValue ? bet.balanceAfter.Value : 0,
                            balanceBefore = bet.balanceBefore.HasValue ? bet.balanceBefore.Value : 0,
                            totalEvents = bet.totalEvents,
                            firstTs = Convert.ToDateTime(bet.firstTs),
                            ts = Convert.ToDateTime(bet.ts),
                            isTest = bet.isTest ? 1 : 0,
                            insertedAt = Convert.ToDateTime(bet.insertedAt),
                            recoveryType = string.IsNullOrWhiteSpace(bet.recoveryType) ? string.Empty : bet.recoveryType,
                            totalJpContribution = bet.totalJpContribution,
                            totalJpWin = bet.totalJpWin,
                        };

                        var result = _apiSkywindBLL.InsertWager(betSkywind);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.roundId} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, bool DoQuick)> UnsettleCheck(int siteId)
        {
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartyBySite(TpId, siteId);

                var checkList = await _apiSkywindBLL.GetUnsettleCheckList();
                var doQuick = checkList.Count() == 1000;
                foreach (var chcek in checkList)
                {
                    try
                    {
                        string[] sArray = Regex.Split(chcek.TransId, "-", RegexOptions.IgnoreCase);
                        //Get wager data
                        var bet = await _apiSkywindBLL.GetBetByTransId(setting["ApiUrl"], setting["SecretKey"], setting["Username"], setting["Password"], sArray[0]);
                        if (bet != null)
                        {
                            var betSkywind = new BetSkywind()
                            {
                                finished = bet.finished ? 1 : 0,
                                revenue = bet.revenue,
                                currency = bet.currency,
                                debit = bet.debit.HasValue ? bet.debit.Value : 0,
                                credit = bet.credit.HasValue ? bet.credit.Value : 0,
                                roundId = bet.roundId,
                                brandId = bet.brandId,
                                playerCode = bet.playerCode,
                                gameCode = bet.gameCode,
                                device = bet.device,
                                win = bet.win,
                                bet = bet.bet,
                                balanceAfter = bet.balanceAfter.HasValue ? bet.balanceAfter.Value : 0,
                                balanceBefore = bet.balanceBefore.HasValue ? bet.balanceBefore.Value : 0,
                                totalEvents = bet.totalEvents,
                                firstTs = Convert.ToDateTime(bet.firstTs),
                                ts = Convert.ToDateTime(bet.ts),
                                isTest = bet.isTest ? 1 : 0,
                                insertedAt = Convert.ToDateTime(bet.insertedAt),
                                recoveryType = string.IsNullOrWhiteSpace(bet.recoveryType) ? string.Empty : bet.recoveryType,
                                totalJpContribution = bet.totalJpContribution,
                                totalJpWin = bet.totalJpWin,
                            };
                            _apiSkywindBLL.InsertWager(betSkywind);
                        }
                        else
                        {
                            var thirdPartyBet = await _apiSkywindBLL.GetThirdPartyBetByTransId(setting["ApiUrl"], setting["SecretKey"], setting["Username"], setting["Password"], sArray[0]);
                            if (thirdPartyBet.Any())
                            {
                                var betSkywind = new BetSkywind()
                                {
                                    finished = 1,
                                    revenue = thirdPartyBet.Sum(x => x.revenue),
                                    currency = thirdPartyBet.FirstOrDefault().currency,
                                    debit = 0,
                                    credit = 0,
                                    roundId = thirdPartyBet.FirstOrDefault().roundId,
                                    brandId = thirdPartyBet.FirstOrDefault().brandId,
                                    playerCode = thirdPartyBet.FirstOrDefault().playerCode,
                                    gameCode = thirdPartyBet.FirstOrDefault().gameCode,
                                    device = string.Empty,
                                    win = thirdPartyBet.Sum(x => x.win),
                                    bet = thirdPartyBet.Sum(x => x.bet),
                                    balanceAfter = 0,
                                    balanceBefore = 0,
                                    totalEvents = thirdPartyBet.Count.ToString(),
                                    firstTs = Convert.ToDateTime(thirdPartyBet.Min(x => x.insertedAt)),
                                    ts = Convert.ToDateTime(thirdPartyBet.Min(x => x.insertedAt)),
                                    isTest = thirdPartyBet.FirstOrDefault().isTest ? 1 : 0,
                                    insertedAt = Convert.ToDateTime(thirdPartyBet.Max(x => x.insertedAt)),
                                    recoveryType = string.Empty,
                                    totalJpContribution = 0,
                                    totalJpWin = 0,
                                };
                                _apiSkywindBLL.InsertWager(betSkywind);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var msg = $"INSERT_WAGER(WAGERID: {chcek.TransId} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(chcek)}\r\n{ex.Message}");
                    }
                }

                _logger.LogInformation($"Skywind Settle Check Success.");
                return (true, doQuick);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Skywind Settle Check FATAL_ERROR.\r\n{ex.Message}");
                return (false, true);
            }
        }
    }
}

