using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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
        
        public int MigrationCustomerInfoIns(MigrationCustomerRequest request , MySqlConnection conn, MySqlTransaction trans , out int custId)
        {
            var p = new DynamicParameters(new
            {
                _SiteId = request.SiteId,
                _UserName = request.UserName,
                _AccountName = request.AccountName,
                _Userpwd = request.Userpwd,
                _CurrencyId =request.CurrencyId,
                _UserLang = request.UserLang,
                _RegTime = request.RegTime,
                _Status = request.Status,
                _AgentId = request.AgentId,
                _AllAgentId = request.AllAgentId,
                _SignupUrl = request.SignupUrl ?? "",
                _ReferralUrl = request.ReferralUrl ?? "",
                _DeviceType = (int)request.DeviceType,
                _SignupIp = request.SignupIp ?? "",
                _VipLevel = request.VipLevel == 0 ? 1 : request.VipLevel,
            });
            p.Add("_CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Service_MigrationCustomerInfo_Ins", p ,conn ,trans);
            custId = p.Get<int>("_CustId");
            var resultCode = p.Get<int>("_ResultCode");
            
            return resultCode;
        }
        
        public int MigrationCustomerProfileIns(int custId ,MigrationCustomerRequest request , MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                _CustId = custId,
                _SiteId = request.SiteId,
                _Email = string.IsNullOrEmpty(request.Email) ? null : request.Email,
                _Profile = string.IsNullOrEmpty(request.Profile) ? "{}" : request.Profile,
                _PhoneNo = string.IsNullOrEmpty(request.PhoneNo) ? null : request.PhoneNo,
                _AreaCode = string.IsNullOrEmpty(request.AreaCode) ? null : request.AreaCode,
                _PhoneVerified = request.PhoneVerified,
                _PhoneVerifiedTime = request.PhoneVerified ? request.PhoneVerifiedTime : null,
                _EmailVerified = request.EmailVerified,
                _EmailVerifiedTime = request.EmailVerified ? request.EmailVerifiedTime : null,
                _Address = request.Address ?? "{}"
            });
            p.Add("_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Service_MigrationCustProfile_Ins", p ,conn ,trans);
            var resultCode = p.Get<int>("_ResultCode");
            
            return resultCode;
        }
        
        public int MigrationCustomerPaymentIns(int custId ,MigrationCustomerRequest request , MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                _CustId = custId,
                _SiteId = request.SiteId,
                _CustBankDetail = request.CustBankDetail.Any() ? JsonConvert.SerializeObject(request.CustBankDetail) : "{}",
                _CustEWalletDetail = request.CustEWalletDetail.Any() ? JsonConvert.SerializeObject(request.CustEWalletDetail) : "{}" ,
                _CustCryptoWalletDetail = request.CustCryptoWalletDetail.Any() ? JsonConvert.SerializeObject(request.CustCryptoWalletDetail) : "{}",
                _FirstDepositTime = request.FirstDepositTime,
                _DepositSum = request.DepositSum,
                _DepositCnt = request.DepositCnt,
                _WithdrawlSum = request.WithdrawlSum,
                _WithdrawlCnt = request.WithdrawlCnt,
                _EffectiveBetSum = request.EffectiveBetSum
            });
            p.Add("_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            
            _mainDbClient.Execute("Service_MigrationCustPayment_Ins", p ,conn ,trans);
            var resultCode = p.Get<int>("_ResultCode");
            
            return resultCode;
        }
        
        public int MigrationCustomerBalanceIns(MigrationBalanceRequest request)
        {
            var p = new DynamicParameters(new
            {
                _CustId = request.CustId,
                _SiteId = request.SiteId,
                _Balance = request.Balance,
                _WithdrawalLimit = request.WithdrawalLimit
            });
            p.Add("_ResultCode", dbType: DbType.Int32, direction: ParameterDirection.Output);
            
            _mainDbClient.Execute("Service_MigrationCustBalance_Ins",CommandType.StoredProcedure, p);
            var resultCode = p.Get<int>("_ResultCode");
            
            return resultCode;
        }
        
        public async Task<bool> MigrationAffiliateIns(MigrationAffiliateRequest request)
        {
            var custIdList = request.AffiliatedCustIdList.Select(cust => new AffiliateCustList {CustId = cust}).ToList();

            var p = new DynamicParameters(new
            {
                _SiteId = request.SiteId,
                _CustId = request.CustId,
                _AffiliateCode = request.AffiliateCode,
                _AffiliateName = request.AffiliateName,
                _ParentAffiliate =request.ParentAffiliate,
                _AffiliatedCustIdList = JsonConvert.SerializeObject(custIdList)
            });
            
            var result = _mainDbClient.Execute("Service_MigrationAffiliate_Ins", CommandType.StoredProcedure, p) > 0;
            
            return result;
        }
        
        public bool ExistsPhoneNo(int custId, int siteId, string AreaCode, string PhoneNo)
        {
            return _mainDbClient.ExecuteScalar<int>("Member_ExistsPhoneNo_Get", new { CustId = custId, AreaCode, PhoneNo, SiteId = siteId }) != 0;
        }
        
    }
}
