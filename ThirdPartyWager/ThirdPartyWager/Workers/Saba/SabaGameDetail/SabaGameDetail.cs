using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class SabaGameDetail : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SabaGameDetail> _logger;
        private readonly SabaGameDetailBLL _sabaGameDetailBll;
        private Timer _timerSabaGameDetail;
        private const string TpName = "SabaGameDetail";

        public SabaGameDetail(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<SabaGameDetail>>();
            _sabaGameDetailBll = provider.GetRequiredService<SabaGameDetailBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerSabaGameDetail = new Timer(DoSabaGameDetailAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        
        private async void DoSabaGameDetailAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", TpName);
            LogContext.PushProperty("JobName", nameof(DoSabaGameDetailAsync));

            var isError = false;
            try
            {
                isError = await _sabaGameDetailBll.GetSabaGameDetail();
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, GetCurrentAsyncMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timerSabaGameDetail.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSabaGameDetail?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
