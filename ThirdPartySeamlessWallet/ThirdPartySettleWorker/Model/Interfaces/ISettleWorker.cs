using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySettleWorker.Model.Interfaces
{
    interface ISettleWorker : IHostedService
    {
        Tp Tp { get; }
    }
}
