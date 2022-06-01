namespace ThirdParty.PT
{
    public partial class TpHelper
    {
        private class WithdrawParam : DepositParam
        {
            public bool isForce { get; set; }
            public bool losebonus { get; set; }
        }
    }
}