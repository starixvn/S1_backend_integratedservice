using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.MG
{
    public class MGBLL : IMGBLL
    {
        private readonly IApiMGBLL _mgApi;
        private readonly IApiSWBLL _swApi;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly List<string> _currency = new List<string> {"VND", "USD", "INR", "IDR", "MYR", "THB"};
        private readonly Log2File log2File;

        public MGBLL(IServiceProvider provider)
        {
            _mgApi = provider.GetRequiredService<IApiMGBLL>();
            _swApi = provider.GetRequiredService<IApiSWBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            var config = _thirdPartyBLL.GetThirdPartySetting((int)Tp.MG);
            _mgApi.MgConfig = config;
            log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task ReSettle()
        {
            foreach (var currencyId in _currency)
            {
                var token = await _mgApi.GetToken(currencyId);
                if(string.IsNullOrEmpty(token)) return;
                var mgFailedTransactions = await _mgApi.GetFailedTransactionsAsync(token, currencyId);
                if (mgFailedTransactions != null && mgFailedTransactions.Count > 0)
                {
                    foreach (var transaction in mgFailedTransactions)
                    {
                        var request = new MGUpdatebalanceRequest()
                        {
                            txnType = transaction.actionType.ToUpper(),
                            txnEventType = transaction.txnEventType,
                            playerId = transaction.playerId,
                            amount = transaction.amount,
                            currency = currencyId,
                            txnId = transaction.txnId,
                            creationTime = DateTime.UtcNow.Ticks,
                            contentCode = transaction.contentCode,
                            betId = transaction.betId,
                            completed = transaction.completed,
                            channel = transaction.channel,
                            IsFailedTransaction = true
                        };
                    
                        var isSuccess = await _swApi.MgUpdateBalance(request);
                        if (isSuccess)
                        {
                            log2File.Info($"currency:{currencyId},transaction {JsonConvert.SerializeObject(request)}", "MG_ReSettle");
                            await _mgApi.UpdateFailedTransactionToResolvedState(token, transaction.txnId, currencyId);
                        }
                    }
                }
                Thread.Sleep(10000);
            }
        }
    }
}