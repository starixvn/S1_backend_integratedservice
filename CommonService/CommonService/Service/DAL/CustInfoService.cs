using CommonService.Service.Model;
using CommonService.Service.Library;
using CommonService.Service.Model.enums;

namespace CommonService.Service.DAL
{
    public class CustInfoService
    {
        private readonly MySqlClient _mainDbClient;
      
        public CustInfoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            return _mainDbClient.QueryFirst<CustInfoProfile>("Service_CustProfile_Get", new { CustId = custId });
        }
    }
}
