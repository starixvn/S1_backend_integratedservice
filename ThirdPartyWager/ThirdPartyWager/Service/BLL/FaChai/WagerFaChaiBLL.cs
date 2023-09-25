using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using Org.BouncyCastle.Math.EC.Multiplier;
using ThirdPartyWager.Service.BLL.IM;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Workers;

namespace ThirdPartyWager.Service.BLL.FaChai
{
    public class WagerFaChaiBLL
    {
        private readonly ILogger<WagerFaChaiBLL> _logger;
        private readonly ApiFaChaiBLL _apiFaChaiBLL;
        private readonly WagerFaChaiService _wagerFaChaiService;
        public WagerFaChaiBLL(IServiceProvider provider)
        {
            _logger = provider.GetService<ILogger<WagerFaChaiBLL>>();
            _apiFaChaiBLL = provider.GetRequiredService<ApiFaChaiBLL>();
            _wagerFaChaiService = provider.GetRequiredService<WagerFaChaiService>();
        }

        public async Task GetWagerAsync(Dictionary<string, string> setting, string startTime, string endTime, string currency)
        {
            string msg;
            var recordList = await _apiFaChaiBLL.GetRecordListAsync(setting, startTime, endTime, currency);

            if (recordList.Records != null)
            {
                foreach (var record in recordList.Records)
                {
                    try
                    {
                        await _wagerFaChaiService.InsertWager(record);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(recordID: {record.recordID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(record)}\r\n{ex.Message}");
                    }
                }
            }
        }

        public async Task<bool> GetWagerRedoAsync(Dictionary<string, string> setting, string startTime, string endTime, string currency)
        {
            string msg;
            var recordList = await _apiFaChaiBLL.GetHistoryRecordListAsync(setting, startTime, endTime, currency);

            if (recordList.Records != null)
            {
                foreach (var record in recordList.Records)
                {
                    try
                    {
                        await _wagerFaChaiService.InsertWager(record);
                    }
                    catch(Exception ex)
                    {
                        msg = $"INSERT_WAGER(recordID: {record.recordID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(record)}\r\n{ex.Message}");
                    }
                }
            }

            return true;
        }
    }
}
