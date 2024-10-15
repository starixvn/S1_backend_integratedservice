using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using ThirdPartySeamlessWallet.Service.BLL;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class CRMController : ControllerBase
    {
        private readonly CRMConfig _CRMConfig;
        private readonly CRMBLL _cRMBLL;
        public CRMController(IServiceProvider provider)
        {
            _CRMConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<CRMConfig>("CRM");
            _cRMBLL = provider.GetRequiredService<CRMBLL>();
        }
        public class CRMConfig 
        {
            public string ApiUrlv2 { get; set; }
            public string ApiKey { get; set; } 
        }

        public class ScheduledCampaignsEventData
        {
            public int EventTypeID { get; set; }
            public DateTime TimeStamp { get; set; }
            public int CampaignID { get; set; }
            public int ChannelID { get; set; }
        }
        public class Personalization
        {
            public Dictionary<string, string> PersonalInfo { get; set; }
            public Dictionary<string, string> EventName { get; set; }
        }

        public class TriggeredCampaignsEventData
        {
            public string CustomerID { get; set; }
            public bool IsVisitor { get; set; }
            public int ActionID { get; set; }
            public List<string> PromoCode { get; set; }
            public int ChannelID { get; set; }
            public string CampaignID { get; set; }
            public string TemplateName { get; set; }
            public string CampaignSeriesID { get; set; }
            public int TemplateID { get; set; }
            public string Timestamp { get; set; }
            public string Notes { get; set; }
            public List<string> Tags { get; set; }
            public Personalization Personalization { get; set; }
            public string ActionName { get; set; }
            public string TriggerName { get; set; }
            public string TargetGroupName { get; set; }
            public string BrandID { get; set; }
        }
        public class Channel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Campaign
        {
            public int TestGroupSize { get; set; }
            public int ControlGroupSize { get; set; }
            public List<Channel> Channels { get; set; }
            public List<string> Tags { get; set; }
            public int TargetGroupID { get; set; }
            public string CampaignType { get; set; }
            public int Duration { get; set; }
            public int LeadTime { get; set; }
            public string Notes { get; set; }
            public bool IsMultiChannel { get; set; }
            public bool IsRecurrence { get; set; }
            public string Status { get; set; }
            public string Error { get; set; }
        }
        public class CustomerAction
        {
            public string CustomerID { get; set; }
            public int ActionID { get; set; }
            public int ChannelID { get; set; }
            public string TemplateID { get; set; }
            public string ScheduleTime { get; set; }  // 可以改用 DateTime 類型
            public string PromoCode { get; set; }
            public List<string> CustomerAttributes { get; set; }
        }
        [HttpPost]
        public IActionResult ScheduledCampaigns(ScheduledCampaignsEventData req)
        {
            var campaignID = req.CampaignID;
            //var campaignDetail = GetCampaignDetail(campaignID);
            var campaignCustomers = GetCampaignCustomers(campaignID);
            SetCustPromotion(campaignCustomers);
            return Ok();
        }

        private Campaign GetCampaignDetail(int campaignID)
        {
            var options = new RestClientOptions($"{_CRMConfig.ApiUrlv2}/Actions/GetCampaignDetails?CampaignID={campaignID}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("X-API-KEY", _CRMConfig.ApiKey);
            var response = client.Get(request);
            return JsonConvert.DeserializeObject<Campaign>(response.Content);
        }

        private List<CustomerAction> GetCampaignCustomers(int campaignID)
        {
            var options = new RestClientOptions($"{_CRMConfig.ApiUrlv2}/Customers/GetCustomerExecutionDetailsByCampaign?CampaignID={campaignID}");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("X-API-KEY", _CRMConfig.ApiKey);
            var response = client.Get(request);
            return JsonConvert.DeserializeObject<List<CustomerAction>>(response.Content);
        }

        private void SetCustPromotion(List<CustomerAction> customerAction)
        {
            _cRMBLL.SetCustPromotionByScheduledCampaigns(customerAction);
        }


        [HttpPost]
        public IActionResult TriggeredCampaigns(TriggeredCampaignsEventData req)
        {
            _cRMBLL.SetCustPromotionByTriggeredCampaigns(req);
            return Ok();
        }
    }
}
