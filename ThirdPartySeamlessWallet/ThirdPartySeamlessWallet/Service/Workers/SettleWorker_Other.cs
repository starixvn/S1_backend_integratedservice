using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {
        private void DoSettleAWCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.AWC;

            try
            {
                var unsettledList = settledBLL.GetUnsettledListCF_AWC();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.userId);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.platformTxId,
                                GameType = GameType.AWC_Rooster,
                                TpUserName = unsettled.userId,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValidNotRollover
                            };

                            var result = settledBLL.Settled_AWC(tpCustInfo, betRecordInfo);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAWC");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleAWC");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleAWC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
