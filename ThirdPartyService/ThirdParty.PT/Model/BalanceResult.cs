namespace ThirdParty.PT
{
    public partial class TpHelper
    {
        private class BalanceResult
        {
            public string currencycode { get; set; }
            public decimal balance { get; set; }
            public decimal bonusbalance { get; set; }
            // public decimal rc_balance { get; set; }
            // public decimal current_bet { get; set; }
            public string playername { get; set; }
        }
    }
}