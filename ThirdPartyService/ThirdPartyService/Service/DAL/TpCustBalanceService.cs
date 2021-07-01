using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.Library.BonusService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpCustBalanceService
    {
        #region Fields

        private readonly IConfiguration _configuration;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;

        #endregion

        #region Constructors

        public TpCustBalanceService(IConfiguration configuration,
                                 ILogger<MySqlClient> logger)
        {
            _configuration = configuration;
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), logger);
            _reportDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringReport"), logger);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 取得最近一次排程執行結束時間
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public string GetLastestRunTime(string tableDate)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TableDate", tableDate);
            return _reportDbClient.QueryFirst<string>("Service_TpCustBalanceCourse_GetLastestRunTime", parameter);
        }

        /// <summary>
        /// 檢查第三方客戶餘額歷程表是否存在
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public string CheckTpCustBalanceCourseTable(string tableDate)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TableDate", tableDate);
            return _reportDbClient.QueryFirst<string>("Service_TpCustBalanceCourse_CheckTable", parameter);
        }

        /// <summary>
        /// 新增第三方客戶餘額歷程表
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public bool CreateTpCustBalanceCourseTable(string tableDate)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TableDate", tableDate);
            return _mainDbClient.Execute("Service_TpCustBalanceCourse_CreateTable", parameter);
        }

        /// <summary>
        /// 刪除第三方客戶餘額歷程表
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public bool DropTpCustBalanceCourseTable(string tableDate)
        {
            var parameter = new DynamicParameters();
            parameter.Add("_TableDate", tableDate);
            return _mainDbClient.Execute("Service_TpCustBalanceCourse_DropTable", parameter);
        }

        /// <summary>
        /// 新增餘額
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <param name="timeString">所屬日期時間</param>
        /// <param name="custBalance">客戶餘額資料</param>
        /// <returns></returns>
        public int InsertBalance(string tableDate, string timeString, TpCustBalanceModel custBalance)
        {
            var parameter = new DynamicParameters();
            int affectCount = 0;

            parameter.Add("_TableDate", tableDate);
            parameter.Add("_Time", timeString);
            parameter.Add("_TpId", custBalance.TpId);
            parameter.Add("_CustId", custBalance.CustId);
            parameter.Add("_SiteId", custBalance.SiteId);
            parameter.Add("_CurrencyId", custBalance.CurrencyId);
            parameter.Add("_Balance", custBalance.Balance);
            parameter.Add("AffectCount", affectCount, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);

            bool result = _mainDbClient.Execute("Service_TpCustBalanceCourse_Set", parameter);

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
