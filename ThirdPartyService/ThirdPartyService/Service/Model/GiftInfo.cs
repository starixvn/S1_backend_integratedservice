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

    public int GiftType { get; set; }

    public int Round { get; set; }
}