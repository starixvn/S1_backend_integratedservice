using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpCreditFailService
    {
        private readonly MySqlClient _mainDbClient;

        public TpCreditFailService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public IEnumerable<TpCreditFail> GetCreditFail()
        {
            return _mainDbClient.Query<TpCreditFail>("Service_TpRequestFailureRecord_Get");
        }
        
        public void SetCreditFail(RequestFailureType type, Tp tpId, string request, string errorMessage)
        {
            _mainDbClient.Execute("Service_TpRequestFailureRecord_Ins", parameters: new { _Type = (int) type ,_TpId = (int) tpId , _Request = request , _ErrorMessage = errorMessage});
        }
        
        public void DelCreditFail(int id)
        {
            _mainDbClient.Execute("Service_TpRequestFailureRecord_Del" , parameters: new { _Id = id });
        }
        
        
    }
}