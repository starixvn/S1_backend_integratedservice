using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static CommonService.Service.Model.OTPSMS;

namespace CommonService.Service.DAL
{
    public class OTPSMSService
    {
        private readonly IConfiguration configuration;

        public OTPSMSService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public SendOTPSMSResult GetOTPVerifyinfo(int custId,string sOTPNum)
        {
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, OTPNum = sOTPNum, TimeOutMin = 5});
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.InputOutput);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

             conn.Execute("Service_OTPVerifyinfo_Ins", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;

            return result;
        }

        public int? VerifyOPT(VerifyOTPReq verifyOTPReq)
        {
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = verifyOTPReq.CustId, OTPNumInput = verifyOTPReq.InputOTP });

            return conn.QueryFirstOrDefault<int?>("Service_OTPVerifyinfo_Get", p, commandType: CommandType.StoredProcedure);

        }
    }
}
