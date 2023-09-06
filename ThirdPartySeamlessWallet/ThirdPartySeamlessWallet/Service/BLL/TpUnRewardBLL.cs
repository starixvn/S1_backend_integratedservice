using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpUnRewardBLL
    {
        private readonly TpUnRewardService _tpUnRewardService;

        public TpUnRewardBLL(IServiceProvider provider)
        {
            _tpUnRewardService = provider.GetRequiredService<TpUnRewardService>();
        }

        public IEnumerable<TResult> GetTpUnRewardData<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return _tpUnRewardService.GetTpUnRewardDataBySpName<TResult>(spName, parameters, dataBase);
        }

        public int SetTpUnRewardData(string spName, object parameters, DataBase dataBase)
        {
            return _tpUnRewardService.SetTpUnRewardDataBySpName(spName, parameters, dataBase);
        }

        public IEnumerable<TpUnReward> GetTpUnRewardList(Tp tp, GameType gameType, int LimitCount = 200)
        {
            return _tpUnRewardService.GetTpUnRewardList(tp, gameType, LimitCount);
        }

        public int SetTpUnReward(Tp tp, GameType gameType, string transId, string tpUserName, bool isSuccess = true)
        {
            return _tpUnRewardService.SetTpUnReward(tp, gameType, transId, tpUserName, isSuccess);
        }
    }
}
