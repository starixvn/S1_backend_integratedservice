using Dapper;
using System.Data;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpCustBalanceService
    {
        #region Fields
        
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        #endregion

        #region Constructors

        public TpCustBalanceService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 取得最近一次排程執行結束時間
        /// </summary>
        /// <returns></returns>
        public string GetLatestRunTime()
        {
            var parameter = new DynamicParameters();
            return _reportDbClient.QueryFirst<string>("Service_TpCustBalanceHistory_GetLatestRunTime", parameter);
        }

        /// <summary>
        /// 新增餘額
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <param name="timeString">所屬日期時間</param>
        /// <param name="custBalance">客戶餘額資料</param>
        /// <returns></returns>
        public int InsertBalance(string timeString, TpCustBalanceModel custBalance)
        {
            var parameter = new DynamicParameters();
            var affectCount = 0;

            parameter.Add("_Time", timeString);
            parameter.Add("_TpId", custBalance.TpId);
            parameter.Add("_CustId", custBalance.CustId);
            parameter.Add("_SiteId", custBalance.SiteId);
            parameter.Add("_CurrencyId", custBalance.CurrencyId);
            parameter.Add("_Balance", custBalance.Balance);
            parameter.Add("_RequestTime", custBalance.RequestTime);
            parameter.Add("_ResponseTime", custBalance.ResponseTIme);
            parameter.Add("AffectCount", affectCount, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);

            var result = _mainDbClient.Execute("Service_TpCustBalanceHistory_Set", parameter) > 0;

            if (!result)
            {
                return 0;
            }
            else
            {
                return parameter.Get<int?>("AffectCount") ?? 0;
            }
        }

        #endregion
    }
}
