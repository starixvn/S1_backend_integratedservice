using System.Xml.Serialization;

namespace ThirdParty.HL
{ 
    public class HLConfig
    {
        public string ApiUrl { get; set; }
        public string ApiUser { get; set; }
        public string ApiPass { get; set; }
        public string AgentUser { get; set; }
        public string AgentPass { get; set; }
    }
    
    public class HLReqBase
    {
        public string ApiUser { get; set; }
        public string ApiPass { get; set; }
        public string AgentUser { get; set; }
        public string AgentPass { get; set; }
    }
    
    #region Request

    public class CreateMemberReq : HLReqBase
    {
        public string PlayerID { get; set; }
        public string PlayerPass { get; set; }
        public string PlayerName { get; set; }
    }
        
    public class DepositReq : HLReqBase
    {
        public string PlayerID { get; set; }
        public Decimal DepositAmount { get; set; }
    }
        
    public class WithdrawReq : HLReqBase
    {
        public string PlayerID { get; set; }
        public Decimal WithdrawAmount { get; set; }
    }

    public class CheckTransferReq : HLReqBase
    {
        public string PlayerID { get; set; }
    }
    
    public class GetLoginInformationReq : HLReqBase
    {
        public string PlayerID { get; set; }
        public string PlayerPass { get; set; }
    }

    public class LoginReq
    {
        public string PlayerID { get; set; }
        public string SessionID { get; set; }
        public string TokenCode { get; set; }
    }
    
    
        
    #endregion
    
    #region Response
    
    [XmlRoot(ElementName="response")]
    public class HLResponse 
    { 

        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 

        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; } 
    }

    public class BetLoginResponse : HLResponse
    {
        [XmlElement(ElementName="sessionID")] 
        public string SessionID { get; set; } 

        [XmlElement(ElementName="tokenCode")] 
        public string TokenCode { get; set; } 
    }
    
    
    
    #endregion
    
    #region GetBetList
    
    [XmlRoot(ElementName="response")]
    public class Response : HLResponse
    {
        [XmlElement(ElementName="betData")]
        public List<BetData> BetDataList { get; set; }
    }

    public class BetData
    {
        [XmlElement(ElementName="betID")]
        public long BetID { get; set; }

        [XmlElement(ElementName="dateTime")]
        public DateTime DateTime { get; set; }

        [XmlElement(ElementName="drawType")]
        public int DrawType { get; set; }

        [XmlElement(ElementName="drawDate")]
        public DateTime DrawDate { get; set; }

        [XmlElement(ElementName="betNumber")]
        public string BetNumber { get; set; }

        [XmlElement(ElementName="betPosition")]
        public string BetPosition { get; set; }

        [XmlElement(ElementName="bet")]
        public decimal Bet { get; set; }
    }
    
    #endregion
}