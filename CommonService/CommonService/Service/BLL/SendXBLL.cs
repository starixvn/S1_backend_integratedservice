using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class SendXBLL
    {
        private readonly IOptions<SendXConfig> sendXConfig;
        private readonly HttpHelper httpHelper;
        private readonly CustInfoBLL custInfoBLL;
        private readonly Dictionary<string, string> headerParams;

        private class Result
        {
            public int status { get; set; }
            public string message { get; set; }
            public string data { get; set; }
        }

        public SendXBLL(IOptions<SendXConfig> sendXConfig, HttpHelper httpHelper, CustInfoBLL custInfoBLL)
        {
            this.sendXConfig = sendXConfig;
            this.httpHelper = httpHelper;
            this.custInfoBLL = custInfoBLL;
            headerParams = new Dictionary<string, string> {
                { "api_key", sendXConfig.Value.ApiKey}
            };
        }
        public class IdentifyParams
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string userName { get; set; }
            public string email { get; set; }
            public List<string> tags { get; set; }
        }
        public async Task<ApiResult> IdentifyAsync(CustInfoProfile custInfo)
        {
            ApiResult apiResult = new ApiResult();

            string url = $@"{sendXConfig.Value.basePath}/contact/identify?team_id={sendXConfig.Value.TeamId}";
            IdentifyParams identifyParams = new IdentifyParams
            {
                email = custInfo.Email,
                firstName = custInfo.UserName,
                lastName = "",
                userName = custInfo.UserName,
                tags = new List<string>()
            };

            identifyParams.tags.Add($"{custInfo.SiteName}_Welcome_{custInfo.UserLang}");
            var result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(identifyParams), "application/json");

            var resultResp= JsonConvert.DeserializeObject<Result>(result);

            if (resultResp.status == 200)
            {
                apiResult.ErrorCode = 0;
                apiResult.RawMessage = resultResp.message;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
                apiResult.RawMessage = resultResp.message;
            }

            return apiResult;
        }
        public class TrackParams
        {
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<string> addTags { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public List<string> removeTags { get; set; }
        }
        public async Task<ApiResult> trackAsync(CustInfoProfile custInfo)
        {
            ApiResult apiResult = new ApiResult();

            string url = $@"{sendXConfig.Value.basePath}/contact/track?team_id={sendXConfig.Value.TeamId}&email={custInfo.Email}";
            TrackParams trackParams = new TrackParams
            {
                removeTags = new List<string>
            {
                $"{custInfo.SiteName}_Welcome_{custInfo.UserLang}"
            }
            };
            var result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(trackParams), "application/json");

            trackParams = new TrackParams
            {
                addTags = new List<string>
            {
                $"{custInfo.SiteName}_Welcome_{custInfo.UserLang}"
            }
            };
            result = await httpHelper.HttpPostAsync(url, headerParams, JsonConvert.SerializeObject(trackParams), "application/json");

            var resultResp = JsonConvert.DeserializeObject<Result>(result);

            if (resultResp.status == 200)
            {
                apiResult.ErrorCode = 0;
                apiResult.RawMessage = resultResp.message;
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = resultResp.message;
                apiResult.RawMessage = resultResp.message;
            }
            return apiResult;
        }
    }
}
