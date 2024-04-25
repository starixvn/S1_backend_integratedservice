using ThirdPartyCore.Model.enums;

namespace ThirdPartyService.Service.Model;

public class GiftInfo
{
    public int SiteId { get; set; }
    public string SiteName { get; set; }
    public int CurrencyId { get; set; }
    public string TpCurrencyId { get; set; }
    public int GiftId { get; set; }
    public int CategoryId { get; set; }
    public GeneralStatusEnum Status { get; set; }
    public int Point { get; set; }
    public decimal PriceWorth { get; set; }
    public bool ShowStock { get; set; }
    public int Stock { get; set; }
    public int Sort { get; set; }

    public GiftTypeEnum GiftType { get; set; }

    public int Round { get; set; }
}

public class WorkerPeriodTask
{
    public int SiteId { set; get; }
    public int CurrencyId { set; get; }
    public int PeriodType { get; set; }
    public int PeriodDay { get; set; }
    public DateTime SpecificDate { get; set; }
    public int RedeemHours { set; get; }
    public DateTime SettleTime { set; get; }
    public string TimeZone { set; get; }
    public DateTime CurrencyNowTime { set; get; }
    public DateTime LastCurrencySettledTime { get; set; }
}

public enum GeneralStatusEnum
{
    Disable = 0,
    Enable = 1
}

public enum GiftTypeEnum
{
    Common = 1,
    FreeSpin = 2
}