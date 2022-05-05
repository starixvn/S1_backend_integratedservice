using System;
using System.Collections.Generic;
using System.Net.Http;
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
    public class TelegramController : Controller
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<TelegramController> _logger;
        private static Dictionary<string, Dictionary<string, string>> _telegram;

        public TelegramController(IServiceProvider provider, IConfiguration configuration)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _telegram = configuration.GetSection("Telegram").Get<Dictionary<string, Dictionary<string, string>>>();
            _logger = provider.GetService<ILogger<TelegramController>>();
        }

        public class TelegaramRequest
        {
            public string GroupName { get; set; }
            public string Messge { get; set; }
        }

        [HttpPost]
        public async Task<bool> SendMessage([FromBody] TelegaramRequest req)
        {
            try
            {
                var group = _telegram.ContainsKey(req.GroupName) ? _telegram[req.GroupName] : throw new Exception("GroupName");
                var url = $"{_telegram["Common"]["Url"]}/bot{group["Token"]}/sendMessage";

                var param = new
                {
                    chat_id = group["ChatId"],
                    text = req.Messge
                };

                _ = await HttpPostAsync(url, JsonConvert.SerializeObject(param));
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return false;
            }
        }

        [HttpPost]
        public async Task<string> GetUpdates([FromBody] TelegaramRequest req)
        {
            try
            {
                var group = _telegram.ContainsKey(req.GroupName) ? _telegram[req.GroupName] : throw new Exception("GroupName");
                var url = $"{_telegram["Common"]["Url"]}/bot{group["Token"]}/getUpdates";

                return await HttpPostAsync(url, "");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return "";
            }
        }

        private async Task<string> HttpPostAsync(string url, string parameters)
        {
            try
            {
                var result = await _httpHelper.HttpPost1Async(url, null, parameters, "application/json");
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
