using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CommonService.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SendXController : ControllerBase
    {
        private readonly IOptions<SendXConfig> sendXConfig;
        private readonly HttpHelper httpHelper;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Dictionary<string, string> headerParams;

        public SendXController(IOptions<SendXConfig> sendXConfig, HttpHelper httpHelper, CustInfoBLL custInfoBLL)
        {
            this.sendXConfig = sendXConfig;
            this.httpHelper = httpHelper;
            this.custInfoBLL = custInfoBLL;
            headerParams = new Dictionary<string, string> {
                { "api_key", sendXConfig.Value.ApiKey}
            };
        }

        public class IdentifyReq
        {
            public int custId { get; set; }
        }

        public class IdentifyParams
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string userName { get; set; }
            public string email { get; set; }
            public List<string> tags { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> IdentifyAsync(IdentifyReq identifyReq)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(identifyReq.custId);
            if (custInfo != null)
            {
                string url = $@"{sendXConfig.Value.basePath}/contact/identify?team_id={sendXConfig.Value.TeamId}";
                IdentifyParams identifyParams = new IdentifyParams();
                identifyParams.email = custInfo.Email;
                identifyParams.firstName = custInfo.UserName;
                identifyParams.lastName = "";
                identifyParams.userName = custInfo.UserName;
                identifyParams.tags = new List<string>();
                identifyParams.tags.Add($"{custInfo.SiteName}_Welcome_{custInfo.UserLang}");
                var result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(identifyParams), "application/json");
                apiResult.ErrorCode = 0;
                apiResult.RawMessage = result;
            }
            return apiResult;
        }

        public class TrackReq
        {
            public int custId { get; set; }
        }

        public class TrackParams
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<string> addTags { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<string> removeTags { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult> trackAsync(TrackReq trackReq)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(trackReq.custId);
            //custInfo = new CustInfoProfile();
            //custInfo.Email = "celine.saw99@gmail.com";
            if (custInfo != null)
            {
                string url = $@"{sendXConfig.Value.basePath}/contact/track?team_id={sendXConfig.Value.TeamId}&email={custInfo.Email}";
                TrackParams trackParams = new TrackParams();
                trackParams.removeTags = new List<string>();
                trackParams.removeTags.Add($"{custInfo.SiteName}_Welcome_{custInfo.UserLang}");
                //trackParams.addTags.Add($"first sign up");
                var result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(trackParams), "application/json");

                trackParams = new TrackParams();
                //trackParams.removeTags = new List<string>();
                //trackParams.removeTags.Add($"internal test");
                trackParams.addTags = new List<string>();
                trackParams.addTags.Add($"{custInfo.SiteName}_Welcome_{custInfo.UserLang}");
                //trackParams.addTags.Add($"first sign up");
                result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(trackParams), "application/json");

                apiResult.ErrorCode = 0;
                apiResult.RawMessage = result;
            }
            return apiResult;
        }
    }
}