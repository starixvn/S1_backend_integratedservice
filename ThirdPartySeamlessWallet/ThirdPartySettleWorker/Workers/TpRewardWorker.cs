using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySettleWorker.Model.Exceptions;

namespace ThirdPartySettleWorker.Workers
{
    public class TpRewardWorker : BackgroundService
    {
        // timer{gametype}
        private Timer timer57;
        private Timer timer67;

        private readonly SettledBLL _settledBLL;
        private readonly TpUnRewardBLL _tpUnRewardBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<TpRewardWorker> _logger;

        public TpRewardWorker(IServiceProvider provider)
        {
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpUnRewardBLL = provider.GetRequiredService<TpUnRewardBLL>();
            _logger = provider.GetRequiredService<ILogger<TpRewardWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //timer57 = new Timer(DoReward, new TaskParam { Tp = Tp.MG, GameType = GameType.MG_RNG, TpCategory = TpCategory.Slots, SpName = "Service_Reward_MG_Settle_Get" }, TimeSpan.Zero, TimeSpan.Zero);
            timer67 = new Timer(DoReward, new TaskParam { Tp = Tp.AP, GameType = GameType.AP_RNG, TpCategory = TpCategory.Slots, SpName = "Service_RewardRNG_AP_Settle_Get" }, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private class TaskParam
        {
            public Tp Tp { get; set; }
            public GameType GameType { get; set; }
            public TpCategory TpCategory { get; set; }
            public string SpName { get; set; }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timer57?.Change(Timeout.Infinite, 0);
            timer67?.Change(Timeout.Infinite, 0);
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
            var spName = param.SpName;
            var forContinue = false;
            var isError = false;
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
                        var data = _tpUnRewardBLL.GetTpUnRewardData<SettleRewardData>(spName, new { TransId = tpUnReward.TransId }, DataBase.Main).First();
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
                            TransTime = data.SettleTime,
                            SettlementType = SettlementType.SettledForValid,
                        };

                        var result = _settledBLL.DoTpRewardTicket(tpCustInfo, tpRewardRecordInfo, tpCategory);
                        if (result == 1)
                        {
                            Task.Run(() =>
                            {
                                _tpUnRewardBLL.SetTpUnReward(tp, tpGameType, tpUnReward.TransId, isSuccess: true);
                            });
                        }
                    }
                    catch (Exception ex1)
                    {
                        Task.Run(() =>
                        {
                            _tpUnRewardBLL.SetTpUnReward(tp, tpGameType, tpUnReward.TransId, isSuccess: false);
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
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                switch (tpGameType)
                {
                    case GameType.MG_RNG:
                        timer57?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    case GameType.AP_RNG:
                        timer67?.Change(dueTimeSpan, TimeSpan.Zero);
                        break;
                    default:
                        throw new UnRewardNotFoundException($"{tpGameType} GameType not found.");
                        break;
                }
            }
        }
    }
}
