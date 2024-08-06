using Serilog.Context;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.Model;
namespace ThirdPartyService.Service.Worker
{
    public class CreateBonusCodeWorker : BackgroundService
    {
        private readonly ILogger<CreateBonusCodeWorker> _logger;
        private readonly GiftInfoBLL _giftInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer _timerCreateBonusCode;
        private readonly string WorkerName = "CreateBonusCode";
        private readonly int[] _numbers;

        public CreateBonusCodeWorker(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<CreateBonusCodeWorker>>();
            _giftInfoBLL = provider.GetRequiredService<GiftInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _numbers = new int[] { 5, 10 };//提供創建的三方Id
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timerCreateBonusCode = new Timer(DoCreateBonusCodeTaskAsync, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoCreateBonusCodeTaskAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            _logger.LogInformation($"{WorkerName} start");
            var isError = false;
            try
            {
                var workList = _giftInfoBLL.GetWorkList(WorkerName);
                var giftList = _giftInfoBLL.GetGiftList(_numbers).Where(x => x.GiftType == GiftTypeEnum.FreeSpin);

                foreach (var periodTask in workList)
                {
                    if (periodTask.SettleTime.Date >= DateTime.UtcNow.Date) continue;//判斷今天是否執行過

                    var todoList = giftList.Where(x => x.SiteId == periodTask.SiteId && x.CurrencyId == periodTask.CurrencyId);

                    foreach (var item in todoList)
                    {
                        _logger.LogInformation($"{item.GiftId} in progress");
                        var bonusCode = BonusCodeRule(item);
                        var inputs = new Dictionary<string, string>
                        {
                            { "GameId", item.GameId },
                            { "BonusCode", bonusCode },
                            { "Round", item.Round.ToString() },
                            { "Currency", item.TpCurrencyId },
                            { "ExpirationDay","8"} //有效期限8天
                        };
                        var p = new CreateBonusCode()
                        {
                            TpId = item.TpId,
                            SiteId = item.SiteId,
                            inputs = inputs
                        };
                        var result = _thirdPartyBLL.CreateBonusCode(p);

                        if (result.ErrorCode == 0)
                        {
                            _giftInfoBLL.SetTpBonusCode(item.TpId, item.SiteId, inputs, result.Message);
                            _logger.LogInformation($"GiftId:{item.GiftId}，BonusCode:{bonusCode} Create Success");
                        }
                        else
                        {
                            _logger.LogInformation($"GiftId:{item.GiftId}，BonusCode:{bonusCode} Create Fail，{result.RawMessage}");
                        }

                    }
                    
                    _giftInfoBLL.SetTaskDone(periodTask.SiteId, periodTask.CurrencyId, WorkerName);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, MethodExtension.GetCurrentAsyncMethod());
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(10);

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(5);
                }

                // 設定定時器
                _timerCreateBonusCode.Change(dueTimeSpan, TimeSpan.Zero);
                _logger.LogInformation($"{WorkerName} End");
            }

        }

        private static string BonusCodeRule(GiftInfo giftInfo)
        {
            //BonusCode規則。ex.52FREESPIN100FORNOVA8820240306
            //排程預先建立隔一日的bonuscode
            var bonusCode = $"{giftInfo.TpId}{giftInfo.CurrencyId}FREESPIN{giftInfo.Round}FOR{giftInfo.SiteName.ToUpper()}{DateTime.UtcNow.AddDays(1).ToString("yyyyMMdd")}";
            return bonusCode;
        }

    }
}
