using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;

namespace CommonService.Service.DAL
{
    public class FaceBookPixelService
    {
        private readonly MySqlClient _mainDbClient;

        public FaceBookPixelService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public CustFaceBookData GetCustFaceBookData(int custId , string transId)
        {
            return _mainDbClient.QueryFirst<CustFaceBookData>("Service_CustFaceBookData_Get" ,new { _CustId = custId , _TransId = transId });
        }
        
        public int DelCustFaceBookData(int custId , string transId)
        {
            return _mainDbClient.Execute("Service_CustFaceBookData_Del" ,new { _CustId = custId , _TransId = transId });
        }
    }
}
