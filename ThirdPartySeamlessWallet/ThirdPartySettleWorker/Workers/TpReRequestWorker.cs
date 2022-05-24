using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Controllers;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class TpReRequestWorker : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly TpRequestFailBLL _tpRequestFailBll;
        private readonly TpUnSettledBLL _tpUnSettledBll;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly SettledBLL _settledBll;
        private readonly AllbetBll _allBetBll;
        private readonly ILogger<TpReRequestWorker> _logger;
        private Timer _evoTimer;
        private Timer _ppTimer;
        private Timer _allBetTimer;
        public TpReRequestWorker(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _tpRequestFailBll = provider.GetRequiredService<TpRequestFailBLL>();
            _tpUnSettledBll = provider.GetRequiredService<TpUnSettledBLL>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _allBetBll = new AllbetBll(provider);
            _logger = provider.GetRequiredService<ILogger<TpReRequestWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _evoTimer = new Timer(DoEvoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            _ppTimer = new Timer(DoPPTask, null, TimeSpan.Zero, TimeSpan.Zero);
            _allBetTimer = new Timer(DoAllBetTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoEvoTask(object state)
        {
            try
            {
                var list = _tpRequestFailBll.GetRequestFail(RequestFailureType.Tp, Tp.EVO);
                foreach (var data in list)
                {
                    var result = 0;

                    switch (data.CategoryId)
                    {
                        case (int)TpRequestFailureCategory.Credit:

                            var reqCredit = JsonConvert.DeserializeObject<BettingData_EVO>(data.Request);
                            var tokenInfoCredit = getTokenInfo(reqCredit.sid);
                            var gameTypeCredit = tokenInfoCredit.gameType;

                            result = gameTypeCredit switch
                            {
                                (int)GameType.Netent_RNG => _settledBll.PayoutRNG_Netent(reqCredit, out _),
                                (int)GameType.RedTiger_RNG => _settledBll.PayoutRNG_RedTiger(reqCredit, out _),
                                _ => _settledBll.PayoutLC_EVO(reqCredit, out _),
                            };

                            break;

                        case (int)TpRequestFailureCategory.Cancel:

                            var req = JsonConvert.DeserializeObject<BettingData_EVO>(data.Request);
                            var tokenInfo = getTokenInfo(req.sid);
                            var gameType = tokenInfo.gameType;

                            result = gameType switch
                            {
                                (int)GameType.Netent_RNG => _settledBll.CancelBetRNG_Netent(req, out _),
                                (int)GameType.RedTiger_RNG => _settledBll.CancelBetRNG_RedTiger(req, out _),
                                _ => _settledBll.CancelBetLC_EVO(req, out _),
                            };

                            break;

                        case (int)TpRequestFailureCategory.PromoPayout:

                            var reqPromoPayout = JsonConvert.DeserializeObject<PromoData_EVO>(data.Request);
                            var tokenInfoPromoPayout = getTokenInfo(reqPromoPayout.sid);
                            var gameTypePromoPayout = tokenInfoPromoPayout.gameType;

                            result = gameTypePromoPayout switch
                            {
                                (int)GameType.Netent_RNG => _settledBll.PromoPayoutRNG_Netent(reqPromoPayout, out _),
                                (int)GameType.RedTiger_RNG => _settledBll.PromoPayoutRNG_RedTiger(reqPromoPayout, out _),
                                _ => _settledBll.PromoPayoutLC_EVO(reqPromoPayout, out _),
                            };

                            break;
                    }

                    if (result > 0 || result == -1)
                    {
                        _tpRequestFailBll.DelRequestFail(data.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                _evoTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoPPTask(object state)
        {
            try
            {
                var list = _tpRequestFailBll.GetRequestFail(RequestFailureType.Tp, Tp.PP);
                foreach (var data in list)
                {
                    var result = 0;
                    var betReq = JsonConvert.DeserializeObject<PPController.BetReq>(data.Request);

                    switch (data.CategoryId)
                    {
                        case (int)TpRequestFailureCategory.EndRound:
                            var gameType = _thirdPartyBll.GetGameType_PP(betReq.gameId);
                            result = _tpUnSettledBll.InsertTpUnsettle(Tp.PP, gameType, betReq.roundId.ToString());
                            break;
                    }

                    if (result > 0)
                    {
                        _tpRequestFailBll.DelRequestFail(data.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                _ppTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        private void DoAllBetTask(object state)
        {
            try
            {
                var list = _tpRequestFailBll.GetRequestFail(RequestFailureType.Tp, Tp.Allbet);
                foreach (var data in list)
                {
                    var result = 0;
                    var betReq = JsonConvert.DeserializeObject<AllBetTransferRequest<AllBetTransferDetails>>(data.Request);
                    var (tpCustInfo, _) = _allBetBll.GetTPInfo(betReq.Player);

                    switch (betReq.Type)
                    {
                        case AllbetTransferType.Settle:
                            result = _allBetBll.Settle(tpCustInfo, betReq, out _);
                            break;
                        case AllbetTransferType.ReSettle:
                            result = _allBetBll.ReSettle(tpCustInfo, betReq, out _);
                            break;
                    }
                    
                    if (result > 0 )
                    {
                        _tpRequestFailBll.DelRequestFail(data.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, GetCurrentMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                _allBetTimer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        
        #region Evo Private Method

        private EVOController.TokenInfo getTokenInfo(string token)
        {
            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return new EVOController.TokenInfo
            {
                shortTimeToken = sessionKeyArr[0], /* 1 min*/
                longTimeToken = sessionKeyArr[1],  /* Long Time*/
                tpUserName = sessionKeyArr[2],
                gameType = Convert.ToInt32(sessionKeyArr[3]),
                tpId = Convert.ToInt32(sessionKeyArr[4])
            };
        }

        #endregion

    }
}
