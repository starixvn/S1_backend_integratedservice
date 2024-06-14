using System.Data;

using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

using Dapper;

namespace CommonService.Service.DAL
{
    public class OTPSMSService
    {
        private readonly MySqlClient _mainDbClient;

        public OTPSMSService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public SendOTPSMSResult GetOTPVerifyinfo(int custId, int type = 0)
        {
            var result = new SendOTPSMSResult();
            var p = new DynamicParameters(new { CustId = custId, _OTPType = type });
            p.Add("Request_id", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Service_OTPVerifyinfo_Get", p);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }

        public SendOTPSMSResult GetSignUpOTPVerifyinfo(string areaPhoneNo, int type = 0)
        {
            var result = new SendOTPSMSResult();
            var p = new DynamicParameters(new { _PhoneNo = areaPhoneNo, _OTPType = type });
            p.Add("Request_id", dbType: DbType.String, direction: ParameterDirection.Output);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Service_SignUpOTPVerifyInfo_Get", p);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }

        
        public SendOTPSMSResult OTPVerifyinfoIns(int custId, string sOtpNum, int type = 0)
        {
            string requestId = null;
            var result = new SendOTPSMSResult();
            var p = new DynamicParameters(new
            {
                CustId = custId,
                _OTPType = type,
                OTPNum = sOtpNum,
                TimeOutMin = 5
            });

            p.Add("Request_id", requestId, dbType: DbType.String, direction: ParameterDirection.InputOutput);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Service_OTPVerifyinfo_Ins", p);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }
        
        public SendOTPSMSResult SignUpOTPVerifyinfoIns(string areaPhoneNo, string sOtpNum, int type = 0)
        {
            string requestId = null;
            var result = new SendOTPSMSResult();
            var p = new DynamicParameters(new
            {
                _PhoneNo = areaPhoneNo,
                _OTPType = type,
                OTPNum = sOtpNum,
                TimeOutMin = 5
            });

            p.Add("Request_id", requestId, dbType: DbType.String, direction: ParameterDirection.InputOutput);
            p.Add("StartUnixTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("OTPstatus", dbType: DbType.Int32, direction: ParameterDirection.Output);

            _mainDbClient.Execute("Service_SignUpOTPVerifyInfo_Ins", p);
            result.Status = p.Get<int?>("OTPstatus") ?? -1;
            result.StartUnixTime = p.Get<long?>("StartUnixTime") ?? 0;
            result.Request_id = p.Get<string>("Request_id");

            return result;
        }

        public int VerifyOTP(VerifyOTPReq verifyOTPReq, int type = 0)
        {
            var p = new DynamicParameters(new
            {
                verifyOTPReq.CustId,
                _OTPType = type,
                OTPNumInput = verifyOTPReq.InputOTP
            });

            p.Add("OTPResult", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Service_VerifyOTPinfo_Get", p);
            var result = p.Get<int>("OTPResult");
            if (result == 0)
            {
                OTPLogVerify(new OTPLog
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
            var result = _mainDbClient.Execute("Service_OTPLog_Ins", oTPLog);
            return result;
        }

        private int OTPLogVerify(OTPLog oTPLog)
        {
            var result = _mainDbClient.Execute("Service_OTPLog_Verify", oTPLog);
            return result;
        }
    }
}
