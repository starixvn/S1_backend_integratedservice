using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySettleWorker.Model.Interfaces
{
    interface ISettleWorker: IHostedService
    {
        Tp Tp { get;}
    }
}
