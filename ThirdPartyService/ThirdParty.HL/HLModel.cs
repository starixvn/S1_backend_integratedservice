using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ThirdParty.HL
{ 
    public class HLConfig
    {
        public string ApiUrl { get; set; }
        public string MobileGameUrl { get; set; }
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

    public class GetProfileReq : HLReqBase
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
    
    [XmlRoot(ElementName="response")]
    public class BetLoginResponse
    {
        
        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 
        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; }
        [XmlElement(ElementName="sessionID")] 
        public string SessionID { get; set; } 
        [XmlElement(ElementName="tokenCode")] 
        public string TokenCode { get; set; } 
    }
    
    [XmlRoot(ElementName="response")]
    public class GetProfileResponse 
    { 

        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 
        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; }
        [XmlElement(ElementName="loginID")] 
        public string LoginID { get; set; } 
        [XmlElement(ElementName="fullName")] 
        public string FullName { get; set; } 
        [XmlElement(ElementName="status")] 
        public string Status { get; set; } 
        [XmlElement(ElementName="balance")] 
        public string Balance { get; set; } 
        [XmlElement(ElementName="outstanding")] 
        public string Outstanding { get; set; } 
        [XmlElement(ElementName="currency")] 
        public string Currency { get; set; } 
    }
    
    
    #endregion
    
    #region TransferCheck
    
    [XmlRoot(ElementName="txnLog")]
    public class TxnLog { 

        [XmlElement(ElementName="dateTime")] 
        public DateTime DateTime { get; set; } 

        [XmlElement(ElementName="mode")] 
        public string Mode { get; set; } 

        [XmlElement(ElementName="previousBalance")] 
        public double PreviousBalance { get; set; } 

        [XmlElement(ElementName="amount")] 
        public double Amount { get; set; } 

        [XmlElement(ElementName="balance")] 
        public double Balance { get; set; } 

        [XmlElement(ElementName="remarks")] 
        public int Remarks { get; set; } 

        [XmlElement(ElementName="ipAddress")] 
        public string IpAddress { get; set; } 
    }

    [XmlRoot(ElementName="response")]
    public class TransferCheckResponse { 

        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 

        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; } 

        [XmlElement(ElementName="txnLog")] 
        public TxnLog TxnLog { get; set; } 
    }
    
    #endregion
    
    #region GetBetList
    
    
    [XmlRoot(ElementName="response")]
    public class GetBetListResponse 
    { 
        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 
        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; }
        [XmlElement(ElementName="betData")]
        public List<BetData> BetDataList { get; set; }
    }
    
    public class BetData
    {
        [XmlElement(ElementName="betID")]
        public string BetID { get; set; }

        [XmlElement(ElementName="dateTime")]
        public string DateTime { get; set; }

        [XmlElement(ElementName="drawType")]
        public int DrawType { get; set; }

        [XmlElement(ElementName="drawDate")]
        public string DrawDate { get; set; }

        [XmlElement(ElementName="betNumber")]
        public string BetNumber { get; set; }

        [XmlElement(ElementName="betPosition")]
        public string BetPosition { get; set; }

        [XmlElement(ElementName="bet")]
        public decimal Bet { get; set; }
    }
    
    #endregion
}