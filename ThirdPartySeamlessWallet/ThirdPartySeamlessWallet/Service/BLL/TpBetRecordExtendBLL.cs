using Dapper;
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

        public void AddTpBetRecordExtend(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo)
        {
            try
            {
                var content = JsonConvert.SerializeObject(new
                {
                    CustId = tpCustInfo.CustId,
                    BetRecordInfo = betRecordInfo,
                });
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                var response = _httpHelper.GetWithoutProxyClient().PostAsync(_apiUrl, httpContent).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var result = JsonConvert.DeserializeObject<ApiResult<TpBetRecordExtend>>(responseBody);
                if (result.ErrorCode == 0)
                {
                    var parameters = new DynamicParameters(new
                    {
                        betRecordInfo.TpId,
                        betRecordInfo.TransId,
                        GameType = (int)betRecordInfo.GameType,
                        tpCustInfo.CustId,
                        tpCustInfo.UserName,
                        tpCustInfo.CurrencyId,
                        tpCustInfo.SiteId,
                        betRecordInfo.TpUserName,
                        betRecordInfo.SettledTime,
                        betRecordInfo.TransTime,
                        betRecordInfo.Stake,
                        ValidBet = betRecordInfo.ValidBet ?? default,
                        Revenue = betRecordInfo.Revenue ?? default,
                        betRecordInfo.ProgressiveBet,
                        betRecordInfo.ProgressiveWin,
                        betRecordInfo.WinlostAmount,
                        SettlementType = (int)betRecordInfo.SettlementType,
                        TicketStatus = (int)betRecordInfo.TicketStatus,
                        betRecordInfo.BonusId,
                        OriginalState = betRecordInfo.OriginalState ?? string.Empty,
                        betRecordInfo.TicketKey,
                    });
                    parameters.AddDynamicParams(result.Data);
                    _mainDbClient.Execute("Service_TpBetRecordExtend_Ins", parameters: parameters);
                }
            }
            catch (AggregateException ex)
            {
                var msg = new StringBuilder(ex.Message);
                foreach (var innerException in ex.InnerExceptions)
                {
                    msg.Append(innerException.Message);
                }
                _logger.LogError(ex, msg.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
