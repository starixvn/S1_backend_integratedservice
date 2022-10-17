namespace ThirdPartySeamlessWallet.Extensions
{
    public static class ExchangeRateExtension
    {
        public static decimal ConvertToMain(this decimal amount, decimal exchangeRate)
        {
            return amount * exchangeRate;
        }
        
        public static decimal ConvertToTp(this decimal amount, decimal exchangeRate)
        {
            return amount / exchangeRate;
        }
    }
}