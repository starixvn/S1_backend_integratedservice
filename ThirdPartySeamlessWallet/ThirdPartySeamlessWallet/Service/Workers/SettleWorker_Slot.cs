using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {

        private void DoSettleCQ9Work(object state)
        {
            bool forContinue = false;
            bool isError = false;

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRNG_CQ9();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            settledBLL.SettledRNG_CQ9(unsettled, out _);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleCQ9");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleCQ9");
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

                timerSettleCQ9.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
