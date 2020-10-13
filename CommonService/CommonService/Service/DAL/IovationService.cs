using CommonService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;

namespace CommonService.Service.DAL
{
    public class IovationService
    {
        private readonly IConfiguration configuration;
        public IovationService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int IovationResultIns(IovationResult iovationResult)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringLog"));
            var p = new
            {
                AccountCode = iovationResult.accountCode,
                Details = JsonConvert.SerializeObject(iovationResult.details),
                UUId = iovationResult.id,
                Reason = iovationResult.reason,
                Result = iovationResult.result,
                StatedIp = iovationResult.statedIp,
                TrackingNumber = iovationResult.trackingNumber
            };
            return conn.Execute("Common_IovationLog_Ins", p, commandType: CommandType.StoredProcedure);
        }
    }
}
