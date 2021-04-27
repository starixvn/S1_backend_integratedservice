using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.ThirdParty.Interface
{
    public interface IThirdPartyService
    {
       Tp ThirdParty { get; set; }
       GameType GameType { get; set; }
    }
}
