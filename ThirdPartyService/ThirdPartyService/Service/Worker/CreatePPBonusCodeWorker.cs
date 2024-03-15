using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;
namespace ThirdPartyService.Service.Worker
{
    public class CreatePPBonusCodeWorker : BackgroundService
    {
        private readonly ILogger<CreatePPBonusCodeWorker> _logger;
        private readonly GiftInfoBLL _giftInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer _timerCreatePPBonusCode;

        public CreatePPBonusCodeWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CreatePPBonusCodeWorker>>();
            _giftInfoBLL = provider.GetRequiredService<GiftInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerCreatePPBonusCode = new Timer(DoCreatePPBonusCodeTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoCreatePPBonusCodeTaskAsync(object state)
        {
            _logger.LogInformation("Create PP FreeSpin BonusCode start");
            try
            {
                var CreateList = _giftInfoBLL.GetGiftList(5).Where(x => x.GiftType == 2);

                foreach (var item in CreateList)
                {
                    _logger.LogInformation($"{item.GiftId} in progress");
                    var bonusCode = BonusCodeRule(item);
                    var inputs = new Dictionary<string, string>
                    {
                        { "GameId", "vs20olympnova88" },
                        { "BonusCode", bonusCode },
                        { "Round", item.Round.ToString() },
                        { "Currency", item.TpCurrencyId }
                    };
                    var p = new CreateBonusCode()
                    {
                        TpId = 5,//PP
                        SiteId = item.SiteId,
                        inputs = inputs
                    };
                    var result = _thirdPartyBLL.CreateBonusCode(p);
                    _logger.LogInformation(result.ErrorCode == 0
                        ? $"GiftId:{item.GiftId}，BonusCode:{bonusCode} Create Success"
                        : $"GiftId:{item.GiftId}，BonusCode:{bonusCode} Create Fail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                // 設定每天的特定時間
                DateTime scheduledTime = DateTime.UtcNow.Date.AddHours(0);

                // 計算距離下一次執行的時間間隔
                TimeSpan delay = scheduledTime - DateTime.Now;
                if (delay < TimeSpan.Zero)
                {
                    scheduledTime = scheduledTime.AddDays(1); // 如果已經超過了今天的指定時間，則推遲到明天的同一時間
                    delay = scheduledTime - DateTime.Now;
                }

                // 設定定時器
                _timerCreatePPBonusCode.Change(delay, TimeSpan.FromDays(1));
                _logger.LogInformation("Create PP FreeSpin BonusCode End");
            }

        }

        private static string BonusCodeRule(GiftInfo giftInfo)
        {
            //BonusCode規則。ex.PPFREESPIN100FORNOVA8820240306
            var bonusCode = $"PPFREESPIN{giftInfo.Round}FOR{giftInfo.SiteName.ToUpper()}{DateTime.UtcNow.ToString("yyyyMMdd")}";
            return bonusCode;
        }

    }
}
