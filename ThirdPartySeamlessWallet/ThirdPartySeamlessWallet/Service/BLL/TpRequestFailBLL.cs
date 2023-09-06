using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpRequestFailBLL
    {
        private readonly TpRequestFailService _tpRequestFailService;

        public TpRequestFailBLL(IServiceProvider provider)
        {
            _tpRequestFailService = provider.GetRequiredService<TpRequestFailService>();
        }
        
        public IEnumerable<TpRequestFail> GetRequestFail(RequestFailureType type, Tp tpId)
        {
            return _tpRequestFailService.GetRequestFail(type, tpId);
        }
        
        public void SetRequestFail(RequestFailureType type , Tp tpId ,TpRequestFailureCategory categoryId, string request, string errorMessage)
        {
            _tpRequestFailService.SetRequestFail(type, tpId, categoryId, request, errorMessage);
        }
        
        public void DelRequestFail(int id)
        {
            _tpRequestFailService.DelRequestFail(id);
        }
    }
}