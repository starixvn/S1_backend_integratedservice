using System.Xml.Serialization;

namespace ThirdPartyWager.Service.Model
{
    [XmlRoot(ElementName="response")]
    public class HLResponse { 

        [XmlElement(ElementName="errorCode")] 
        public int ErrorCode { get; set; } 

        [XmlElement(ElementName="desc")] 
        public object Desc { get; set; } 

        [XmlElement(ElementName="betData")] 
        public List<BetData> BetData { get; set; } 
    }
        
    [XmlRoot(ElementName="betData")]
    public class BetData { 

        [XmlElement(ElementName="betID")] 
        public int BetID { get; set; } 

        [XmlElement(ElementName="dateTime")] 
        public DateTime DateTime { get; set; } 

        [XmlElement(ElementName="drawType")] 
        public int DrawType { get; set; } 

        [XmlElement(ElementName="drawDate")] 
        public DateTime DrawDate { get; set; } 

        [XmlElement(ElementName="betNumber")] 
        public int BetNumber { get; set; } 

        [XmlElement(ElementName="betPosition")] 
        public string BetPosition { get; set; } 

        [XmlElement(ElementName="bet")] 
        public double Bet { get; set; } 
    }
}