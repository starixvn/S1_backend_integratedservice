using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpBetRecordExtendBLL
    {
        private readonly ILogger<TpBetRecordExtendBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly MySqlClient _mainDbClient;
        private readonly string _apiUrl;

        public TpBetRecordExtendBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpBetRecordExtendBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _apiUrl = $"{provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService")}GetTpBetRecordExtend"; 
        }

        public void AddTpBetRecordExtend(BetRecordInfo BetRecordInfo, TpCustInfo TpCustInfo)
        {
            try
            {         
                var content = JsonConvert.SerializeObject(new
                {
                    CustId = TpCustInfo.CustId,
                    BetRecordInfo = BetRecordInfo,
                });
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");
                var response = _httpHelper.GetClient().PostAsync(_apiUrl, httpContent).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;         
                var result = JsonConvert.DeserializeObject<ApiResult<TpBetRecordExtend>>(responseBody);
                if (result.ErrorCode == 0)
                {       
                    _mainDbClient.Execute("Service_TpBetRecordExtend_Ins", parameters: result.Data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
