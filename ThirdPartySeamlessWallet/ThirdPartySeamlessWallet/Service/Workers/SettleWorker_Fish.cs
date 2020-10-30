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
        private void DoSettleSGFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            //int thisTp = (int)Tp.SG;
            var gameType = GameType.SG_Fish;
            //var category = TpCategory.Fish;
            try
            {
                var unsettledList = settledBLL.GetUnsettledList_SG((int)gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 500;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            settledBLL.SettledFish_SG(unsettled, out _);
                        }
                        catch (Exception ex1)
                        {
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleSGFish");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleSGFish");
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

                timerSettleSGFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
