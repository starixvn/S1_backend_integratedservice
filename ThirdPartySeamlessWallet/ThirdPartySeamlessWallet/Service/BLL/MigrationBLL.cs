using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class MigrationBLL
    {
        private readonly MigrationService _migrationService;
        private readonly MySqlClient _mainDbClient;
            
        public MigrationBLL(IServiceProvider provider)
        {
            _migrationService = provider.GetRequiredService<MigrationService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<(int , int)> MigrationInsFlow(MigrationCustomerRequest request)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var result = _migrationService.MigrationCustomerInfoIns(request, conn, trans , out var custId);
            if (result < 0)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return (-1, result);
            }
            
            var checkPhoneNo = _migrationService.ExistsPhoneNo(custId ,request.SiteId , request.AreaCode , request.PhoneNo);
            if (checkPhoneNo)
            {
                request.PhoneNo = $"{custId.ToString().PadLeft(10, '0')}_{request.PhoneNo}";
            }
        
            result = _migrationService.MigrationCustomerProfileIns(custId, request, conn, trans);
            if (result < 0)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return (-1, result);
            }
            
            result = _migrationService.MigrationCustomerPaymentIns(custId, request, conn, trans);
            if (result < 0)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return (-1, result);
            }
            
            if (result == 0)
            {
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
            }
            
            return (custId, result);
        }
        
        public async Task<bool> Service_MigrationAffiliate_Ins(MigrationAffiliateRequest request)
        {
            return await _migrationService.MigrationAffiliateIns(request);
        }
    }
}
