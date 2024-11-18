using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model.KY
{
    public class BettingDataPK_KY
    {
        public int s { get; set; }
        public string account { get; set; }
        public string orderId { get; set; }
        public string gameNo { get; set; }
        public string gameId { get; set; }
        public string kindId { get; set; }
        public decimal money { get; set; }
        public string currency { get; set; }
        public long timestamp { get; set; }
    }

    public class OrderDataPK_KY
    {
        public int s { get; set; }
        public string orderId {  set; get; }
        public long timestamp {  set; get; }
    }

    public class BettransDataPK_KY : BettingDataPK_KY
    {
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }


    public class BettransData_KY : BettingDataPK_KY
    {
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }

    public class CurrencyData
    {
        public string Deskey { get; set; }
        public string Agent { get; set; }
        public string Md5key { get; set; }
    }

    public class ApiReq
    {
        public string agent { get; set; }
        public long timestamp { get; set; }
        public string param { get; set; }
        public string key { get; set; }
    }

    public class CommonP
    {
        public int s { get; set; }
    }

    public class  GetBalanceP : CommonP
    {
        public string account { get; set; }
    }

    public class KYConfig
    {
        public Dictionary<string, CurrencyData> CurrencyData { get; set; }
    }
}
