using CommonService.Service.Model.enums;

namespace CommonService.Service.Model
{
    #region Config

    public class FaceBookPixelConfig
    {
        public string ApiUrl { get; set; }

        public List<FaceBookPixelKey> Key { get; set; }
        
    }

    public class FaceBookPixelKey
    {
        public int Status { get; set; }
        public string Token { get; set; }

        public string PixelId { get; set; }
    }
    
    #endregion

    #region Request

    public class PixelRequest
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        public int CurrencyId { get; set; }
        public decimal Value { get; set; }
        public string Url { get; set; }
        public List<FaceBookPixel> PixelEvent { get; set; }
    }
    
    #endregion
  
    
    public class PixelEvent
    {
        public string event_name { get; set; }
        public long event_time { get; set; }
        public UserData user_data { get; set; }
        public CustomData custom_data { get; set; } 
        public string event_source_url { get; set; }
        public string action_source { get; set; }
    }

    public class UserData
    {
        public List<string> external_id { get; set; }
    }

    public class CustomData
    {
        public string currency { get; set; }
        public decimal value { get; set; }
     
    }
    
    public class EventData
    {
        public List<PixelEvent> data { get; set; }
    }
  
}
