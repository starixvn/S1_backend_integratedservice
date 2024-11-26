using System.Text;

using Dapper;

using Newtonsoft.Json;

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
            var serviceHost = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost");
            _apiUrl = $"{serviceHost.TpService}GetTpBetRecordExtend";
        }

        public async Task AddTpBetRecordExtend(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo)
        {
            try
            {
                var content = JsonConvert.SerializeObject(new
                {
                    tpCustInfo.CustId,
                    tpCustInfo.CurrencyId,
                    BetRecordInfo = betRecordInfo,
                });
                var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

                var response = await _httpHelper.GetWithoutProxyClient().PostAsync(_apiUrl, httpContent);
                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ApiResult<TpBetRecordExtend>>(responseBody);
                if (result.Data.GameID is null)
                    result.Data.GameID = betRecordInfo.GameID ?? "0";
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
                        GameID = betRecordInfo.GameID ?? "0",
                        BetChoice=betRecordInfo.BetChoice??""
                    });
                    parameters.AddDynamicParams(result.Data);
                    _mainDbClient.Execute("Service_TpBetRecordExtend_Ins_V2", parameters: parameters);
                }
            }
            catch (AggregateException ex)
            {
                var msg = new StringBuilder(ex.Message);
                foreach (var innerException in ex.InnerExceptions)
                {
                    msg.Append(innerException.Message);
                }
                _logger.LogError(ex, $"{msg} || {JsonConvert.SerializeObject(betRecordInfo)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} || {JsonConvert.SerializeObject(betRecordInfo)}");
            }
        }
    }
}
