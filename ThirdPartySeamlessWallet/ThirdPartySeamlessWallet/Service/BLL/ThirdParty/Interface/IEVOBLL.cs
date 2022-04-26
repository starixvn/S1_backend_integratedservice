using System.Collections.Generic;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IEVOBLL
    {
        AMBSlotGetBalanceResponse SlotGetBalance(Dictionary<string, string> request);
    }
}
