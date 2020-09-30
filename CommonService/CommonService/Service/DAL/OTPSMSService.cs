using CommonService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.DAL
{
    public class OTPSMSService
    {
        private readonly IConfiguration configuration;

        public OTPSMSService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public SendOTPSMSResult GetOTPVerifyinfo(int custId)
        {
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId });
            p.Add("Request_id", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Execute("Service_OTPVerifyinfo_Get", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string?>("Request_id") ?? null;

            return result;
        }
        public SendOTPSMSResult OTPVerifyinfoInsWithRequest_id(int custId, string request_id)
        {
            string sOTPNum = null;
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, OTPNum = sOTPNum, TimeOutMin = 5 });
            p.Add("Request_id", request_id, dbType: DbType.String, direction: ParameterDirection.InputOutput);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);


            conn.Execute("Service_OTPVerifyinfo_Ins", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string?>("Request_id") ?? null;

            return result;
        }
        public SendOTPSMSResult OTPVerifyinfoIns(int custId,string sOTPNum)
        {
            string request_id = null;
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, OTPNum = sOTPNum, TimeOutMin = 5});
            p.Add("Request_id",request_id, dbType: DbType.String, direction: ParameterDirection.InputOutput);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);


             conn.Execute("Service_OTPVerifyinfo_Ins", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string?>("Request_id") ?? null;

            return result;
        }
        public int? VerifyOPT(VerifyOTPReq verifyOTPReq)
        {
            SendOTPSMSResult result = new SendOTPSMSResult();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = verifyOTPReq.CustId, OTPNumInput = verifyOTPReq.InputOTP });

            return conn.QueryFirstOrDefault<int?>("Service_VerifyOTPinfo_Get", p, commandType: CommandType.StoredProcedure);

        }
    }
}
