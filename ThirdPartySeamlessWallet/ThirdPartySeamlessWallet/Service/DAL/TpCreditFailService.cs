using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpRequestFailService
    {
        private readonly MySqlClient _mainDbClient;

        public TpRequestFailService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public IEnumerable<TpRequestFail> GetRequestFail(RequestFailureType type, Tp tpId)
        {
            return _mainDbClient.Query<TpRequestFail>("Service_TpRequestFailureRecord_Get", parameters: new { _Type = (int) type ,_TpId = (int) tpId });
        }
        
        public void SetRequestFail(RequestFailureType type, Tp tpId, TpRequestFailureCategory categoryId, string request, string errorMessage)
        {
            _mainDbClient.Execute("Service_TpRequestFailureRecord_Ins", parameters: new { _Type = (int) type ,_TpId = (int) tpId ,_CategoryId = (int) categoryId, _Request = request , _ErrorMessage = errorMessage});
        }
        
        public void DelRequestFail(int id)
        {
            _mainDbClient.Execute("Service_TpRequestFailureRecord_Del" , parameters: new { _Id = id });
        }
        
        
    }
}