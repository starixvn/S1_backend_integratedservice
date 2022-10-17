using System;
using System.Collections.Generic;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.SBO
{
    public interface IApiSBOBLL
    {
        Task<SboWagers> GetBetListByModifyDateAsync(string startTime, string endTime, SBOPortfolio portfolio, SboSetting setting);
    }

    public class ApiSBOBLL : IApiSBOBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly ILogger<ApiSBOBLL> _logger;
        public ApiSBOBLL(HttpHelper httpHelper, ILogger<ApiSBOBLL> logger)
        {
            _httpHelper = httpHelper;
            _logger = logger;
        }

        public async Task<SboWagers> GetBetListByModifyDateAsync(string startTime, string endTime, SBOPortfolio portfolio, SboSetting setting)
        {
            var betListApi = $"{setting.ApiUrl}/web-root/restricted/report/get-bet-list-by-modify-date.aspx";
            var param = new
            {
                CompanyKey = setting.CompanyKey,
                ServerId = setting.ServerId,
                Username = setting.Agent,
                Portfolio = portfolio.ToString(),
                StartDate = startTime,
                EndDate = endTime
            };
            try
            {

                var stringContent = new StringContent(
                    JsonConvert.SerializeObject(param), 
                    Encoding.UTF8, 
                    Application.Json);
                
                var client = _httpHelper.GetClient();
                var httpResponseMessage = await client.PostAsync(betListApi, stringContent);
                
                var content = await httpResponseMessage.Content.ReadAsStringAsync();
                if (!httpResponseMessage.IsSuccessStatusCode)
                {
                    _logger.LogError($"{betListApi} get bet list failed, request:{JsonConvert.SerializeObject(param)} response:{content}");
                    return null;
                }
                var sboWagers = JsonConvert.DeserializeObject<SboWagers>(content);
                return sboWagers;
            }
            catch (Exception e)
            {
                _logger.LogError(e ,$"{betListApi} get bet list failed, request:{JsonConvert.SerializeObject(param)} exception:{e.Message}");
                return null;
            }
        }
    }
}