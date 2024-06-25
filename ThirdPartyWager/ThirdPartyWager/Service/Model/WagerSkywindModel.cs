/// <summary>
/// Summary description for Class1
/// </summary>
namespace ThirdPartyWager.Service.Model
{
    public class WagerSkywindModel
    {
        public bool finished { get; set; }
        public decimal revenue { get; set; }
        public string currency { get; set; }
        public decimal? debit { get; set; }
        public decimal? credit { get; set; }
        public string roundId { get; set; }
        public string brandId { get; set; }
        public string playerCode { get; set; }
        public string gameCode { get; set; }
        public string device { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public decimal? balanceBefore { get; set; }
        public decimal? balanceAfter { get; set; }
        public string totalEvents { get; set; }
        public string firstTs { get; set; }
        public string ts { get; set; }
        public bool isTest { get; set; }
        public string insertedAt { get; set; }
        public string recoveryType { get; set; }
        public decimal totalJpContribution { get; set; }
        public decimal totalJpWin { get; set; }
    }

    public class BetSkywind
    {
        public int finished { get; set; }
        public decimal revenue { get; set; }
        public string currency { get; set; }
        public decimal debit { get; set; }
        public decimal credit { get; set; }
        public string roundId { get; set; }
        public string brandId { get; set; }
        public string playerCode { get; set; }
        public string gameCode { get; set; }
        public string device { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public decimal balanceBefore { get; set; }
        public decimal balanceAfter { get; set; }
        public string totalEvents { get; set; }
        public DateTime firstTs { get; set; }
        public DateTime ts { get; set; }
        public int isTest { get; set; }
        public DateTime insertedAt { get; set; }
        public string recoveryType { get; set; }
        public decimal totalJpContribution { get; set; }
        public decimal totalJpWin { get; set; }
    }
    public class SkywindUnsettleCheck
    {
        public string TransId { get; set; }
    }
    public class WagerSkywindThridPartyModel
    {
        public string extTrxId { get; set; }
        public decimal win { get; set; }
        public decimal bet { get; set; }
        public string currency { get; set; }
        public string brandId { get; set; }
        public string playerCode { get; set; }
        public string gameCode { get; set; }
        public decimal? balanceBefore { get; set; }
        public decimal? balanceAfter { get; set; }
        public bool isTest { get; set; }
        public decimal revenue { get; set; }
        public string insertedAt { get; set; }
        public string roundId { get; set; }
        public string gameProviderCode { get; set; }
        public bool finished { get; set; }
        public int eventId { get; set; }
        public int? providerSpecificData { get; set; }
    }
}

