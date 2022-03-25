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
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class TpReCreditWorker: BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly TpCreditFailBLL _tpCreditFailBll;
        private readonly SettledBLL _settledBll;
        private readonly ILogger<TpReCreditWorker> _logger;
        private Timer _timer;
        
        public TpReCreditWorker(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _tpCreditFailBll = provider.GetRequiredService<TpCreditFailBLL>();
            _settledBll = provider.GetRequiredService<SettledBLL>();
            _logger = provider.GetRequiredService<ILogger<TpReCreditWorker>>();
        }
        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoTask, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private void DoTask(object state)
        {
            try
            {
                var list = _tpCreditFailBll.GetCreditFail();
                
                foreach (var data in list)
                {
                    switch (data.TpId)
                    {
                        case (int) Tp.EVO:
                            
                            var req = JsonConvert.DeserializeObject<BettingData_EVO>(data.Request);
                            var tokenInfo = getTokenInfo(req.sid);
                            var gameType = tokenInfo.gameType;
                            
                            var result = gameType switch
                            {
                                (int)GameType.Netent_RNG => _settledBll.PayoutRNG_Netent(req, out _),
                                (int)GameType.RedTiger_RNG => _settledBll.PayoutRNG_RedTiger(req, out _),
                                _ => _settledBll.PayoutLC_EVO(req, out _),
                            };

                            if (result > 0 || result == -1)
                            {
                                _tpCreditFailBll.DelCreditFail(data.Id);
                            }
                            
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                _timer.Change(dueTimeSpan, TimeSpan.Zero);
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