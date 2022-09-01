using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;

namespace ThirdPartySettleWorker.Workers
{
    public class TpRewardWorker : BackgroundService
    {
        private readonly SettledBLL _settledBLL;
        private readonly TpUnRewardBLL _tpUnRewardBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<TpRewardWorker> _logger;
        private readonly Dictionary<GameType, Timer> _timerList = new Dictionary<GameType, Timer>();
        private readonly IEnumerable<TaskParam> _taskList;

        public TpRewardWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnRewardBLL = provider.GetRequiredService<TpUnRewardBLL>();
            _logger = provider.GetRequiredService<ILogger<TpRewardWorker>>();
            _taskList = new List<TaskParam>()
            {
                new TaskParam { Tp = Tp.AP, GameType = GameType.AP_RNG, TpCategory = TpCategory.Slots, GetSpName = "Service_RewardRNG_AP_Settle_Get", SetSpName = "Service_RewardRNG_AP_Settle_Set" },
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var task in _taskList)
            {
                try
                {
                    var timer = new Timer(DoReward, task, TimeSpan.Zero, TimeSpan.Zero);
                    _timerList.Add(task.GameType, timer);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error: WorkerService");
                }
            }

            return Task.CompletedTask;
        }

        private class TaskParam
        {
            public Tp Tp { get; set; }
            public GameType GameType { get; set; }
            public TpCategory TpCategory { get; set; }
            public string GetSpName { get; set; }
            public string SetSpName { get; set; }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var timer in _timerList)
            {
                timer.Value?.Change(Timeout.Infinite, 0);
            }
            return Task.CompletedTask;
        }

        private void DoReward(object state)
        {
            var param = JsonConvert.DeserializeObject<TaskParam>(JsonConvert.SerializeObject(state));
            var tp = param.Tp;
            var tpId = (int)tp;
            var tpGameType = param.GameType;
            var tpCategory = param.TpCategory;
            var tpName = tp.ToString();
            var getSpName = param.GetSpName;
            var setSpName = param.SetSpName;
            var forContinue = false;
            var isError = false;
            var database = DataBase.Main;
            LogContext.PushProperty("TP", tpName);
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());

            try
            {
                var tpUnRewardList = _tpUnRewardBLL.GetTpUnRewardList(tp, tpGameType);
                if (!tpUnRewardList.Any())
                {
                    throw new UnsettleNotFoundException($"{tpName} no reward data.");
                }

                forContinue = tpUnRewardList.Count() == 200;

                foreach (var tpUnReward in tpUnRewardList)
                {
                    try
                    {
                        var data = _tpUnRewardBLL.GetTpUnRewardData<SettleRewardData>(getSpName, new { TransId = tpUnReward.TransId, TpUserName = tpUnReward.TpUserName }, database).First();
                        if (data == null)
                        {
                            throw new UnRewardNotFoundException($"{tpUnReward.TransId} TransId not found.");
                        }

                        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, data.Account);
                        if (tpCustInfo == null)
                        {
                            throw new UnRewardNotFoundException($"{data.Account} TpCustInfo not found.");
                        }

                        var tpRewardRecordInfo = new TpRewardRecordInfo
                        {
                            TpId = tpId,
                            Amount = data.Amount,
                            TransId = data.TransId,
                            GameType = tpGameType,
                            TpUserName = data.Account,
                            SettledTime = data.SettleTime,
                            TransTime = data.TransTime,
                            SettlementType = SettlementType.SettledForValid,
                        };

                        var result = _settledBLL.DoTpRewardTicket(tpCustInfo, tpRewardRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnRewardBLL.SetTpUnReward(tp, tpGameType, tpUnReward.TransId, data.Account, isSuccess: true);
                                _tpUnRewardBLL.SetTpUnRewardData(setSpName, new { TransId = tpUnReward.TransId, TpUserName = data.Account }, database);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnRewardBLL.SetTpUnReward(tp, tpGameType, tpUnReward.TransId, tpUnReward.TpUserName, isSuccess: false);
                        });
                        isError = true;
                        _logger.LogError(ex1, $"exMessage:{ex1.Message}");
                    }
                }
            }
            catch (UnRewardNotFoundException ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(5);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(5);
                }

                _timerList[tpGameType]?.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
