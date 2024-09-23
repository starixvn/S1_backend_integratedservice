using System.Data;
using Dapper;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class MigrationService
    {
        private readonly MySqlClient _mainDbClient;

        public MigrationService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<( int , int)> MigrationCustomerIns(MigrationCustomerRequest request, int agent ,string allAgent)
        {
            var p = new DynamicParameters(new
            {
                _SiteId = request.SiteId,
                _UserName = request.UserName,
                _Userpwd = request.Userpwd,
                _CurrencyId =request.CurrencyId,
                _UserLang = request.UserLang,
                _RegTime = request.RegTime,
                _Status = request.Status,
                _AgentId = agent,
                _AllAgentId = allAgent,
                _Email = string.IsNullOrEmpty(request.Email) ? null : request.Email,
                _Profile = string.IsNullOrEmpty(request.Profile) ? "{}" : request.Profile,
                _PhoneNo = string.IsNullOrEmpty(request.PhoneNo) ? null : request.PhoneNo,
                _AreaCode = string.IsNullOrEmpty(request.AreaCode) ? null : request.AreaCode,
                _PhoneVerified = request.PhoneVerified,
                _EmailVerified = request.EmailVerified,
                _Address = request.Address ?? "{}",
                _SignupUrl = request.SignupUrl ?? "",
                _ReferralUrl = request.ReferralUrl ?? "",
                _DeviceType = (int)request.DeviceType,
                _SignupIp = request.SignupIp ?? "",
                _Balance = request.Balance,
                _WithdrawalLimit = request.WithdrawalLimit,
                _FirstDepositTime = request.FirstDepositTime
            });
            p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Service_MigrationCustomer_Ins", CommandType.StoredProcedure, p);
            var custId = p.Get<int>("_CustId");
            var resultCode = p.Get<int>("_ResultCode");
            
            return ( custId ,resultCode);
        }
        
    }
}
