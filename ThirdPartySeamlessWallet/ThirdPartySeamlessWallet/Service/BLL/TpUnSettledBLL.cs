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

        [Obsolete("應該修改為從TpUnsettle取出第三方待結算資料，再逐筆取得第三方的結算明細資料")]
        public IEnumerable<TResult> GetTpUnsettledList<TResult>(string spName, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.GetUnsettledListBySpName<TResult>(spName, dataBase);
        }

        /// <summary>
        /// 從TpUnsettle取出第三方待結算資料
        /// </summary>
        public IEnumerable<TpUnsettle> GetTpUnsettleList(Tp tp, GameType gameType, int LimitCount = 1000)
        {
            return _tpUnsettleService.GetTpUnsettleList(tp, gameType, LimitCount);
        }

        /// <summary>
        /// 使用SP取得第三方結算明細資料
        /// </summary>
        public IEnumerable<TResult> GetTpUnsettleData<TResult>(string spName, object parameters, DataBase dataBase)
        {
            return _tpUnsettleService.GetTpUnsettleDataBySpName<TResult>(spName, parameters, dataBase);
        }

        /// <summary>
        /// 更新TpUnsettle的結算結果
        /// </summary>
        public int SetTpUnsettle(Tp tp, GameType gameType, string transId, bool isSuccess = true)
        {
            return _tpUnsettleService.SetTpUnsettle(tp, gameType, transId, isSuccess);
        }

        /// <summary>
        /// 更新第三方Table結算結果
        /// </summary>
        public int SetSettleFlag(string spName, string transId, bool isSuccess = true, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, new { transId, isSuccess }, dataBase);
        }

        /// <summary>
        /// 更新第三方Table結算結果
        /// </summary>
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

        /// <summary>
        /// 更新第三方Table結算結果
        /// </summary>
        public int SetSettleFlag(string spName, object parameters, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, parameters, dataBase);
        }

        /// <summary>
        /// 新增待結算單號到TpUnsettle
        /// </summary>
        public int InsertTpUnsettle(Tp tp, GameType gameType, string transId)
        {
            return _tpUnsettleService.InsertTpUnsettle(tp, gameType, transId);
        }

        /// <summary>
        /// 更新第三方Table結算結果
        /// </summary>
        public int SetSettleFlag(string spName, Tp tp, GameType gameType, string transId, string roundId, bool isSuccess = true, DataBase dataBase = DataBase.Main)
        {
            return _tpUnsettleService.SetSettleFlag(spName, new
            {
                TpId = (int)tp,
                GameType = (int)gameType,
                TransId = transId,
                IsSuccess = isSuccess,
                RoundId = roundId
            }, dataBase);
        }
    }
}
