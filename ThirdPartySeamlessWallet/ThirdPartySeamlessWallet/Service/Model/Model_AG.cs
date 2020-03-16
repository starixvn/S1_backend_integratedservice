using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ThirdPartySeamlessWallet.Service.Model
{
    [XmlRoot("Record")]
    public class Record
    {
        [XmlElement(ElementName = "sessionToken")]
        public string SessionToken { get; set; }
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }
        [XmlElement(ElementName = "playname")]
        public string Playname { get; set; }
        [XmlElement(ElementName = "agentCode")]
        public string AgentCode { get; set; }
        [XmlElement(ElementName = "transactionID")]
        public string Round { get; set; }
        [XmlElement(ElementName = "gametype")]
        public string Gametype { get; set; }
        [XmlElement(ElementName = "gameCode")]
        public string GameCode { get; set; }
        [XmlElement(ElementName = "tableCode")]
        public string TableCode { get; set; }
        [XmlElement(ElementName = "transactionType")]
        public string TransactionType { get; set; }
        [XmlElement(ElementName = "transactionCode")]
        public string TransactionCode { get; set; }
    }

    [XmlRoot(ElementName = "Data")]
    public class Data
    {
        [XmlElement(ElementName = "Record")]
        public Record Record { get; set; }
    }

    [XmlRoot("Record")]
    public class BetRecord
    {
        [XmlElement(ElementName = "sessionToken")]
        public string SessionToken { get; set; }
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }
        [XmlElement(ElementName = "playname")]
        public string Playname { get; set; }
        [XmlElement(ElementName = "agentCode")]
        public string AgentCode { get; set; }
        [XmlElement(ElementName = "betTime")]
        public string BetTime { get; set; }
        [XmlElement(ElementName = "transactionID")]
        public string TransactionID { get; set; }
        [XmlElement(ElementName = "platformType")]
        public string PlatformType { get; set; }
        [XmlElement(ElementName = "round")]
        public string Round { get; set; }
        [XmlElement(ElementName = "gametype")]
        public string Gametype { get; set; }
        [XmlElement(ElementName = "gameCode")]
        public string GameCode { get; set; }
        [XmlElement(ElementName = "tableCode")]
        public string TableCode { get; set; }
        [XmlElement(ElementName = "transactionType")]
        public string TransactionType { get; set; }
        [XmlElement(ElementName = "transactionCode")]
        public string TransactionCode { get; set; }
        [XmlElement(ElementName = "deviceType")]
        public string DeviceType { get; set; }
        [XmlElement(ElementName = "playtype")]
        public string Playtype { get; set; }
    }

    [XmlRoot(ElementName = "Data")]
    public class BetData
    {
        [XmlElement(ElementName = "Record")]
        public BetRecord Record { get; set; }
    }

    [XmlRoot(ElementName = "Record")]
    public class PayoutRecord
    {
        [XmlElement(ElementName = "sessionToken")]
        public string SessionToken { get; set; }
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }
        [XmlElement(ElementName = "netAmount")]
        public string NetAmount { get; set; }
        [XmlElement(ElementName = "validBetAmount")]
        public string ValidBetAmount { get; set; }
        [XmlElement(ElementName = "playname")]
        public string Playname { get; set; }
        [XmlElement(ElementName = "agentCode")]
        public string AgentCode { get; set; }
        [XmlElement(ElementName = "settletime")]
        public string Settletime { get; set; }
        [XmlElement(ElementName = "billNo")]
        public string BillNo { get; set; }
        [XmlElement(ElementName = "transactionID")]
        public string TransactionID { get; set; }
        [XmlElement(ElementName = "gametype")]
        public string Gametype { get; set; }
        [XmlElement(ElementName = "gameCode")]
        public string GameCode { get; set; }
        [XmlElement(ElementName = "transactionType")]
        public string TransactionType { get; set; }
        [XmlElement(ElementName = "transactionCode")]
        public string TransactionCode { get; set; }
        [XmlElement(ElementName = "playtype")]
        public string Playtype { get; set; }
        [XmlElement(ElementName = "ticketStatus")]
        public string TicketStatus { get; set; }
        [XmlElement(ElementName = "gameResult")]
        public string GameResult { get; set; }
        [XmlElement(ElementName = "finish")]
        public string Finish { get; set; }
    }

    [XmlRoot(ElementName = "Data")]
    public class PayoutData
    {
        [XmlElement(ElementName = "Record")]
        public PayoutRecord Record { get; set; }
    }
    [XmlRoot(ElementName = "Record")]
    public class RefundRecord
    {
        [XmlElement(ElementName = "ticketStatus")]
        public string TicketStatus { get; set; }
        [XmlElement(ElementName = "sessionToken")]
        public string SessionToken { get; set; }
        [XmlElement(ElementName = "currency")]
        public string Currency { get; set; }
        [XmlElement(ElementName = "value")]
        public string Value { get; set; }
        [XmlElement(ElementName = "playname")]
        public string Playname { get; set; }
        [XmlElement(ElementName = "agentCode")]
        public string AgentCode { get; set; }
        [XmlElement(ElementName = "betTime")]
        public string BetTime { get; set; }
        [XmlElement(ElementName = "transactionID")]
        public string TransactionID { get; set; }
        [XmlElement(ElementName = "billNo")]
        public string BillNo { get; set; }
        [XmlElement(ElementName = "platformType")]
        public string PlatformType { get; set; }
        [XmlElement(ElementName = "round")]
        public string Round { get; set; }
        [XmlElement(ElementName = "gametype")]
        public string Gametype { get; set; }
        [XmlElement(ElementName = "gameCode")]
        public string GameCode { get; set; }
        [XmlElement(ElementName = "tableCode")]
        public string TableCode { get; set; }
        [XmlElement(ElementName = "transactionType")]
        public string TransactionType { get; set; }
        [XmlElement(ElementName = "transactionCode")]
        public string TransactionCode { get; set; }
        [XmlElement(ElementName = "playtype")]
        public string Playtype { get; set; }
    }

    [XmlRoot(ElementName = "Data")]
    public class RefundData
    {
        [XmlElement(ElementName = "Record")]
        public RefundRecord Record { get; set; }
    }

    [XmlRoot(ElementName = "row")]
    public class Row
    {
        [XmlAttribute(AttributeName = "billNo")]
        public string BillNo { get; set; }
        [XmlAttribute(AttributeName = "status")]
        public string Status { get; set; }
    }

    [XmlRoot(ElementName = "result")]
    public class CheckTicketStatus
    {
        [XmlElement(ElementName = "row")]
        public Row Row { get; set; }
    }

}
