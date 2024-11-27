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
        public string TransId { get; set; }
        public int SiteId { get; set; }
        public int CustId { get; set; }
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
        public string fbp { get; set; }
        public string fbc { get; set; }
        public string em { get; set; }
        public string client_ip_address { get; set; }
        public string client_user_agent { get; set; }
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
  
    public class CustFaceBookData
    {
        public string Ip { get; set; }
        public string Fbc { get; set; }
        public string Fbp { get; set; }
        public string UserAgent { get; set; }
        public string IsoCode { get; set; }
        public string Email { get; set; }
    }
}
