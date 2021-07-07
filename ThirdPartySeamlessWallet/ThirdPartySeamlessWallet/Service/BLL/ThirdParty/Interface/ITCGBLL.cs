using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ITCGBLL
    {
        Task<dynamic> SeamlessWallet(string merchantCode, string @params, string sign);
    }
}