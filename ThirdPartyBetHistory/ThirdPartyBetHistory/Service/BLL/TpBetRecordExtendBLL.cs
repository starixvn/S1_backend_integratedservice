using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.Library;
using ThirdPartyBetHistory.Service.Model;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.BLL
{
    public class TpBetRecordExtendBLL
    {
        private readonly ILogger<TpBetRecordExtendBLL> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly IReadOnlyDictionary<int, CurrencyOption> _currencyList;

        public TpBetRecordExtendBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<TpBetRecordExtendBLL>>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _currencyList = provider.GetRequiredService<IEnumerable<CurrencyOption>>().ToDictionary(d => d.CurrencyId, d => d);
        }

        public async Task AddTpBetRecordExtend(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, TpBetRecordExtend tpBetRecordExtend)
        {
            try
            {
                tpBetRecordExtend.Currency = GetCurrencyIsoCode(tpCustInfo.CurrencyId);
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
                    tpBetRecordExtend.ProviderID,
                    tpBetRecordExtend.Currency,
                    tpBetRecordExtend.RefNo,
                    tpBetRecordExtend.GameID,
                    tpBetRecordExtend.Provider,
                    tpBetRecordExtend.Product,
                    tpBetRecordExtend.Category,
                    tpBetRecordExtend.Game,
                    tpBetRecordExtend.Title,
                    tpBetRecordExtend.MatchID,
                    tpBetRecordExtend.BetType,
                    tpBetRecordExtend.BetChoice,
                    tpBetRecordExtend.Result,
                    tpBetRecordExtend.Odds,
                    tpBetRecordExtend.OddsType,
                    tpBetRecordExtend.Platform,
                    tpBetRecordExtend.Live,
                    tpBetRecordExtend.IP,
                });
                _mainDbClient.Execute("Service_TpBetRecordExtend_Ins", parameters: parameters);
            }
            catch (AggregateException ex)
            {
                var msg = new StringBuilder(ex.Message);
                foreach (var innerException in ex.InnerExceptions)
                {
                    msg.Append(innerException.Message);
                }
                _logger.LogError(ex, $"{msg} || { JsonConvert.SerializeObject(betRecordInfo)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} || { JsonConvert.SerializeObject(betRecordInfo)}");
            }
        }

        private string GetCurrencyIsoCode(int currencyId)
        {
            return _currencyList.GetValueOrDefault(currencyId).IsoCode;
        }
    }
}
