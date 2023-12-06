namespace ThirdPartySeamlessWallet.Extensions
{
    public static class IntExtension
    {
        public static bool IsBetween(this int item, int start, int end)
        {
            return item >= start && item <= end;
        }
    }
}
