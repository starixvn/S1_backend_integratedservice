using Serilog.Context;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Extensions;
using ThirdPartyService.Service.BLL;
using ThirdPartyService.Service.Model;
namespace ThirdPartyService.Service.Worker
{
    public class CreatePPBonusCodeWorker : BackgroundService
    {
        private readonly ILogger<CreatePPBonusCodeWorker> _logger;
        private readonly GiftInfoBLL _giftInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private Timer _timerCreatePPBonusCode;
        private readonly string WorkerName = "CreatePPBonusCode";

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
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            _logger.LogInformation($"{WorkerName} start");
            var isError = false;
            try
            {
                var workList = _giftInfoBLL.GetWorkList(WorkerName);
                var giftList = _giftInfoBLL.GetGiftList((int)TpEnum.PP).Where(x => x.GiftType == GiftTypeEnum.FreeSpin);

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
                            : $"GiftId:{item.GiftId}，BonusCode:{bonusCode} Create Fail，{result.RawMessage}");
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
                _timerCreatePPBonusCode.Change(dueTimeSpan, TimeSpan.Zero);
                _logger.LogInformation($"{WorkerName} End");
            }

        }

        private static string BonusCodeRule(GiftInfo giftInfo)
        {
            //BonusCode規則。ex.PPFREESPIN100FORNOVA8820240306
            //排程預先建立隔一日的bonuscode
            var bonusCode = $"PPFREESPIN{giftInfo.Round}FOR{giftInfo.SiteName.ToUpper()}{DateTime.UtcNow.AddDays(1).ToString("yyyyMMdd")}";
            return bonusCode;
        }

    }
}
