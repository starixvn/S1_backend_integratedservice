using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model.enums
{
    public enum FinancialType
    {
        [Description("Deposit")]
        Deposit = 1,                //1 and 101 ~ 1000
        BonusReward = 5,
        [Description("Withdrawal")]
        Withdrawal = 1001,           //1001 ~ 2000
        [Description(description: "")]
        Others = 99999,
    }

}
