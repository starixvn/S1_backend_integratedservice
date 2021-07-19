using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TCGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<TCGBLL> _logger;
        private readonly MySqlClient _wagerDbClient;
        private readonly int _tpId = (int)Tp.TCG;
        private readonly Dictionary<string, string> _setting;

        public TCGBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<TCGBLL>>();
            LogContext.PushProperty("TP", "TCG");
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            var conn = configuration.GetConnectionString("ConnectionStringWager");
            _wagerDbClient = new MySqlClient(conn, log);
            _setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);
            
        }

        public async Task<bool> GetWagersAsync(DateTimeOffset batchTime, string currency)
        {
            try
            {
                var _config = new TCGConfig(_setting, currency);
                var result = await QuerySettleBetHistoryAsync(_config, batchTime);
                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                await Task.WhenAll(tasks);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCG GetWagersAsync Exception.");
                return false;
            }

        }

        private async Task<List<TCGWagerData>> QuerySettleBetHistoryAsync(TCGConfig config, DateTimeOffset batchTime)
        {           
            var result = new List<TCGWagerData>();
            var reportType = config.WagerTypeList.Split(",").Where(i => i != string.Empty);
            foreach (var type in reportType)
            {           
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    var fileUrl = $"{config.WagerUrl}{type}/SETTLED/{batchTime:yyyyMMdd}/{batchTime:yyyyMMddHHmm}_{page:0000}.json";
                    var queryReuslt = DownloadFTPFile(config, fileUrl);
                    if (queryReuslt.list.Count > 0)
                    {
                        result.AddRange(queryReuslt.list);
                    }         
                    page++;
                    totalPage = queryReuslt.page.total;
                }
            }
            return result;
        }

        public TCGQueryWagerObj DownloadFTPFile(TCGConfig config, string fileUrl)
        {
            try
            {      
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(fileUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(config.WagerAccount, config.WagerPassword);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                using Stream s = response.GetResponseStream();
                using StreamReader sr = new StreamReader(s);
                using JsonReader reader = new JsonTextReader(sr);
                JsonSerializer serializer = new JsonSerializer();
                _logger.LogInformation($"TCG: {config.WagerAccount} || FTP Success:{response.StatusDescription}");
                return serializer.Deserialize<TCGQueryWagerObj>(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TCG: {config.WagerAccount} || FTP Fail: {fileUrl} {ex.Message}.");
                return new TCGQueryWagerObj() ;
            }
        }

        private Task<int> InsertWager(TCGWagerData wager)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerTCG_Ins", new DynamicParameters(wager));
        }
    }
}

