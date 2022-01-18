using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoSABLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSABLL _apiSABLL;
        private readonly WagerSAService _wagerSAService;
        private readonly ILogger<WagerRedoSABLL> _logger;

        private readonly int _tpId = (int)Tp.SA;
        private readonly string _tpName = Tp.SA.ToString();

        public WagerRedoSABLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSABLL = provider.GetRequiredService<ApiSABLL>();
            _wagerSAService = provider.GetRequiredService<WagerSAService>();
            _logger = provider.GetRequiredService<ILogger<WagerRedoSABLL>>();
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", _tpName);
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = _wagerSAService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await _apiSABLL.GetBetSummaryByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                var errorCode = getWagerSummary.GetTransactionDetailsResponse != null ? getWagerSummary.GetTransactionDetailsResponse.ErrorMsgId : getWagerSummary.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return false;
                }
                if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Details == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in getWagerSummary.GetTransactionDetailsResponse.Details.TransactionDetails)
                {
                    totalNum += data.Count;
                }
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getWagerSummary)}");
                    return true;
                }

                var dataWager = await _apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsgId : dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }
                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                // insert wager to db
                foreach (var bet in dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail)
                {
                    try
                    {
                        _wagerSAService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.BetID} )";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"RedoWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return false;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError($"RedoWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
