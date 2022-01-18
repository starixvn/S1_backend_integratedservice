using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpUnSettledBLL
    {
        private readonly TpUnsettleService _tpUnsettleService;

        public TpUnSettledBLL(IServiceProvider provider)
        {
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        }

        public IEnumerable<TResult> GetTpUnsettledList<TResult>(string spName, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.GetUnsettledListBySpName<TResult>(spName, dataBase);
        }

        public IEnumerable<TpUnsettle> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount = 200)
        {
            return _tpUnsettleService.GetTpUnsettleList(tp, gameType, LimitCount);
        }

        public IEnumerable<TResult> GetTpUnsettleData<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return _tpUnsettleService.GetTpUnsettleDataBySpName<TResult>(spName, parameters, dataBase);
        }

        public int SetTpUnsettle(Tp tp, GameType gameType, string transId, bool isSuccess = true)
        {
            return _tpUnsettleService.SetTpUnsettle(tp, gameType, transId, isSuccess);
        }

        public int SetSettleFlag(string spName, string transId, bool isSuccess = true, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, new { transId, isSuccess }, dataBase);
        }

        public int SetSettleFlag(string spName, Tp tp, GameType gameType, string transId, bool isSuccess = true, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, new
            {
                TpId = (int)tp,
                GameType = (int)gameType,
                TransId = transId,
                IsSuccess = isSuccess
            }, dataBase);
        }

        public int SetSettleFlag(string spName, object parameters, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, parameters, dataBase);
        }

        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId)
        {
            return _tpUnsettleService.InsertTpUnsettle(tp, gameType, transId);
        }
    }
}
