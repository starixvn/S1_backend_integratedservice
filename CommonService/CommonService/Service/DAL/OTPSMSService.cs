using CommonService.Service.Model;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace CommonService.Service.DAL
{
    public class OTPSMSService
    {
        private readonly IConfiguration configuration;

        public OTPSMSService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public SendOTPSMSResult GetOTPVerifyinfo(int custId, int type = 0)
        {
            var result = new SendOTPSMSResult();
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, _OTPType = type });
            p.Add("Request_id", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Execute("Service_OTPVerifyinfo_Get", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }

        public SendOTPSMSResult OTPVerifyinfoIns(int custId, string sOtpNum, int type = 0)
        {
            string requestId = null;
            var result = new SendOTPSMSResult();
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(
                new
                {
                    CustId = custId,
                    _OTPType = type,
                    OTPNum = sOtpNum,
                    TimeOutMin = 5
                }
                );

            p.Add("Request_id", requestId, dbType: DbType.String, direction: ParameterDirection.InputOutput);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            conn.Execute("Service_OTPVerifyinfo_Ins", p, commandType: CommandType.StoredProcedure);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }

        public int VerifyOTP(VerifyOTPReq verifyOTPReq, int type = 0)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(
                new
                {
                    verifyOTPReq.CustId,
                    _OTPType = type,
                    OTPNumInput = verifyOTPReq.InputOTP
                });

            p.Add("OTPResult", dbType: DbType.Int32, direction: ParameterDirection.Output);
            conn.Execute("Service_VerifyOTPinfo_Get", p, commandType: CommandType.StoredProcedure);
            var result = p.Get<int>("OTPResult");
            if (result == 0)
            {
                OTPLogVerify(new OTPLog()
                {
                    CustId = verifyOTPReq.CustId,
                    OTPType = type,
                    OTPNum = verifyOTPReq.InputOTP
                });
            }
            return result;
        }

        public int InsertOTPLog(OTPLog oTPLog)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Service_OTPLog_Ins", oTPLog, commandType: CommandType.StoredProcedure);
            return result;
        }

        public int OTPLogVerify(OTPLog oTPLog)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Service_OTPLog_Verify", oTPLog, commandType: CommandType.StoredProcedure);
            return result;
        }
    }
}
