using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonService.Service.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class LetsTalkController
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<LetsTalkController> _logger;
        private static Dictionary<string, Dictionary<string, string>> _letsTalk;

        public LetsTalkController(IServiceProvider provider, IConfiguration configuration)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _logger = provider.GetService<ILogger<LetsTalkController>>();
            _letsTalk = configuration.GetSection("LetsTalk").Get<Dictionary<string, Dictionary<string, string>>>();
        }

        public class LetsTalkMsgParam
        {
            public string receiver { get; set; }
            public LetsTalkMsgtext msg { get; set; }

        }

        public class LetsTalkMsgtext
        {
            public string type { get; set; }
            public string text { get; set; }

        }

        public class LetsTalkSignResult
        {
            public int status { get; set; }
            public string message { get; set; }
            public string token { get; set; }

        }

        public class LetsTalkRequest
        {
            public string GroupName { get; set; }
            public string Message { get; set; }
        }

        [HttpPost]
        public async Task<bool> SendMessage([FromBody] LetsTalkRequest req)
        {
            try
            {
                var url = _letsTalk["Common"]["SandMessageURL"];
                var group = _letsTalk.ContainsKey(req.GroupName) ? _letsTalk[req.GroupName] : throw new Exception("GroupName");
                string token = await GetLetsTalkToken(req.GroupName);
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("token");
                }

                var data = new LetsTalkMsgParam();
                data.msg = new LetsTalkMsgtext();
                data.receiver = group["SandMessageRoomID"];
                data.msg.type = "text";
                data.msg.text = req.Message;

                _ = await HttpPostAsync(url, token, JsonConvert.SerializeObject(data));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return false;
        }

        public async Task<string> GetLetsTalkToken(string groupName)
        {
            try
            {
                var url = _letsTalk["Common"]["SignInURL"];
                var group = _letsTalk.ContainsKey(groupName) ? _letsTalk[groupName] : throw new Exception("GroupName");

                var data = new Dictionary<string, string>(){
                    {"id", group["SignInId"]},
                    {"token", group["SignInToken"]}
                };

                var parameters = JsonConvert.SerializeObject(data);
                using var result = await _httpHelper.HttpPost1Async(url, null, parameters, "application/json");

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var ContentResult = await result.Content.ReadAsStringAsync();
                    var resultResp = JsonConvert.DeserializeObject<LetsTalkSignResult>(ContentResult);

                    if (resultResp.status == 1)
                    {
                        return resultResp.token;
                    }
                    else
                    {
                        _logger.LogError($"httpResult error URL : {url} Result:{ContentResult} ");
                    }
                }
                else
                {
                    _logger.LogError($"httpResponse error URL : {url} parameters:{parameters} StatusCode: {result.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return null;
        }

        private async Task<string> HttpPostAsync(string url, string token, string parameters)
        {
            try
            {
                Dictionary<string, string> header = new Dictionary<string, string> {
                    { "Authorization", $"Bearer {token}"}
                };
                var result = await _httpHelper.HttpPost1Async(url, header, parameters, "application/json");
                if (!result.IsSuccessStatusCode)
                {
                    _logger.LogError($"StatusCode={result.StatusCode}, ReasonPhrase={result.ReasonPhrase}");
                }
                return await result.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }
    }
}
