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
            _apiUrl = $"{provider.GetRequiredService<IConfiguration>().GetValue<string>("TpService")}TpBetRecordExtend"; 
        }

        public void AddTpBetRecordExtend(BetRecordInfo BetRecordInfo, TpCustInfo TpCustInfo)
        {
            var httpContent = new StringContent(JsonConvert.SerializeObject(new { BetRecordInfo, TpCustInfo }), Encoding.UTF8, "application/json");
            var response = _httpHelper.GetClient().PostAsync(_apiUrl, httpContent).Result;
            var responseBody = response.Content.ReadAsStringAsync().Result;
            var tpBetRecordExtend = JsonConvert.DeserializeObject<TpBetRecordExtend>(responseBody);
            InsertTpBetRecordExtend(tpBetRecordExtend);
        }

        public int InsertTpBetRecordExtend(TpBetRecordExtend tpBetRecordExtend)
        {
            return _mainDbClient.Execute("Service_TpBetRecordExtend_Ins", parameters: tpBetRecordExtend);
        }




    }
}
