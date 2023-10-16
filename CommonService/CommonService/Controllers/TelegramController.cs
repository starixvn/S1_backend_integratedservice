using CommonService.Service.BLL;
using CommonService.Service.Library;
using CommonService.Service.Model;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class TelegramController : Controller
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<TelegramController> _logger;
        private readonly Dictionary<string, TelegramConfig> _telegramConfig;

        public TelegramController(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            var configBLL = provider.GetRequiredService<ConfigBLL>();
            _telegramConfig = configBLL.GetConfigValuesBySiteName<TelegramConfig>("Telegram");
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

                var notificationGroup = _telegramConfig.Values.FirstOrDefault()
                    ?.TelegramNotification
                    .FirstOrDefault(n => n.GroupName == req.GroupName) ?? throw new Exception("GroupName");

                var url = $"{_telegramConfig.Values.FirstOrDefault()?.TelegramCommon.Url}/bot{notificationGroup.Token}/sendMessage";

                var param = new
                {
                    chat_id = notificationGroup.ChatId,
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
                var notificationGroup = _telegramConfig.Values.FirstOrDefault()
                    ?.TelegramNotification
                    .FirstOrDefault(n => n.GroupName == req.GroupName) ?? throw new Exception("GroupName");

                var url = $"{_telegramConfig.Values.FirstOrDefault()?.TelegramCommon.Url}/bot{notificationGroup.Token}/getUpdates";

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
                    _logger.LogInformation($"StatusCode={result.StatusCode}, ReasonPhrase={result.ReasonPhrase}");
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
