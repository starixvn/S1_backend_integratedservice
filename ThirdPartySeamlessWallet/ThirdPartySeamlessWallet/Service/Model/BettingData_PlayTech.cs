using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_PlayTech
    {
        public string requestId { get; set; }
        public int brandId { get; set; }
        public string playerId { get; set; }
        public string playerSessionId { get; set; }
        public string gameCode { get; set; }
        public int endSession { get; set; }
        public string detailUrl { get; set; } = "";
        public List<Tran_PlayTech> trans { get; set; }
    }

    public class Tran_PlayTech
    {
        public int seq { get; set; }
        public string transId { get; set; }
        public string referenceId { get; set; } = "";
        public decimal amount { get; set; }
        public string transType { get; set; }
        public DateTime transTime { get; set; }
        public string roundId { get; set; }
        public string roundType { get; set; }
        public int endRound { get; set; }
        public string desc { get; set; } = "";
        public decimal jpc { get; set; }
        public decimal jpw { get; set; }
        public List<JpDetail_PlayTech> jpDetails { get; set; }
        public decimal validBet { get; set; } = 0;
        public decimal validWin { get; set; } = 0;
        public AdditionalData_PlayTech additionalData { get; set; }
        public long transTimeUnix
        {
            get
            {
                return new DateTimeOffset(transTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class JpDetail_PlayTech
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public decimal contribution { get; set; }
        public decimal win { get; set; }
    }

    public class AdditionalData_PlayTech
    {
        public string launchAlias { get; set; }
        public string tableId { get; set; }
    }

    public class TpBetFailureRecordInfo_PlayTech
    {
        public decimal CurrentBalance { get; set; }
        public string transId { get; set; }
        public string roundId { get; set; }
        public decimal amount { get; set; }
        public decimal jpc { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal InsufficientBalance { get; set; }
    }

    public class BettransData_PlayTech
    {
        public string transId { get; set; }
        public string referenceId { get; set; }
        public decimal amount { get; set; }
        public decimal jpc { get; set; }
        public decimal jpw { get; set; }
        public string transType { get; set; }
        public DateTime transTime { get; set; }
        public string roundId { get; set; }
        public string roundType { get; set; }
        public int endRound { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }

    public class RewardData_PlayTech
    {
        public string requestId { get; set; }
        public int brandId { get; set; }
        public string playerId { get; set; }
        public string transId { get; set; }
        public string referenceId { get; set; } = "";
        public string referenceName { get; set; } = "";
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string type { get; set; }
        public DateTime transTime { get; set; }
        public string desc { get; set; } = "";
        public string additionalData { get; set; } = "";
        public long transTimeUnix
        {
            get
            {
                return new DateTimeOffset(transTime, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class SettleData_PlayTech : SettleData
    {
        public decimal CancelBet { get; set; }
    }

    public class PlayTechConfig : TpSiteConfig
    {
        public string BrandId { get; set; }
        public string BrandName { get; set; }
        public string SecretKey { get; set; }
    }
}
