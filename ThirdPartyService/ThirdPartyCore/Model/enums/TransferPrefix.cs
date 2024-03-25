using System.ComponentModel;

namespace ThirdPartyCore.Model.enums
{
    public enum TransferPrefix
    {
        [Description("Transfer")]
        T = 0,
        [Description("Auto Transfer")]
        AT = 1,
        [Description("Bonus Cancel Transfer")]
        BT = 2
    }
}
