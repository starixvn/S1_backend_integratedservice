using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.BLL
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

        public void AddTpBetRecordExtend(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, TpBetRecordExtend tpBetRecordExtend)
        {
            try
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
                    betRecordInfo.ValidBet,
                    Revenue = 0,
                    ProgressiveBet = 0,
                    ProgressiveWin = 0,
                    betRecordInfo.WinlostAmount,
                    SettlementType = (int)betRecordInfo.SettlementType,
                    TicketStatus = (int)betRecordInfo.TicketStatus,
                    betRecordInfo.BonusId,
                    OriginalState = betRecordInfo.OriginalState ?? string.Empty,
                    betRecordInfo.TicketKey,
                });
                parameters.AddDynamicParams(tpBetRecordExtend);
                _mainDbClient.Execute("Service_TpBetRecordExtend_Ins", parameters: parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }
    }
}
