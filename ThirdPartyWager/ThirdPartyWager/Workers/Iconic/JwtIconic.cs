using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;
using Microsoft.Extensions.DependencyInjection;

namespace ThirdPartyWager.Workers
{
    public class JwtIconic : BackgroundService
    {
        private readonly ILogger<JwtIconic> _logger;
        private readonly ApiIconicBLL _apiIconicBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly CommonBLL _commonBLL;

        private Timer timer;
        private readonly int tpId = (int)Tp.Iconic;
        private readonly string _tpName = Tp.Iconic.ToString();

        public JwtIconic(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<JwtIconic>>();
            _apiIconicBLL = provider.GetRequiredService<ApiIconicBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTaskJWT, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskJWT(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get setting
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);
                var currencyList = _commonBLL.GetTpCurrencyList(tpId);
                
                // call jwt
                foreach (var curr in currencyList)
                {
                    var jwtUserName = setting[$"agent_{curr.TpCurrencyId}"];
                    var getJwt = await _apiIconicBLL.GetJwtToken(setting["ApiUrl"], jwtUserName, setting["password"]);
                    if (getJwt.token != null)
                    {
                        setting[$"token_{curr.TpCurrencyId}"] = getJwt.token;
                    }
                }

                //update setting
                _thirdPartyBLL.SetThirdPartySetting(tpId, setting);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromDays(7);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
