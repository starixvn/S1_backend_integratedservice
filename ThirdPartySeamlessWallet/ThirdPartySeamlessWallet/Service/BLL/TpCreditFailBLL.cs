using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpCreditFailBLL
    {
        private readonly TpCreditFailService _tpCreditFailService;

        public TpCreditFailBLL(IServiceProvider provider)
        {
            _tpCreditFailService = provider.GetRequiredService<TpCreditFailService>();
        }
        
        public IEnumerable<TpCreditFail> GetCreditFail()
        {
            return _tpCreditFailService.GetCreditFail();
        }
        
        public void SetCreditFail(RequestFailureType type , Tp tpId , string request, string errorMessage)
        {
            _tpCreditFailService.SetCreditFail(type, tpId, request, errorMessage);
        }
        
        public void DelCreditFail(int id)
        {
            _tpCreditFailService.DelCreditFail(id);
        }
    }
}