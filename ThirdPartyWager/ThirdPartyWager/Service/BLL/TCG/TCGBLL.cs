using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Proxy;
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
        private readonly IConfiguration _configuration;

        public TCGBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<TCGBLL>>();
            LogContext.PushProperty("Tp", "TCG");
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            _wagerDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringWager"), log);
            _setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);
            _configuration = configuration;
            
        }

        public async Task<bool> GetWagersAsync<T>(DateTimeOffset batchTime, string currency)
        {
            try
            {
                var _config = new TCGConfig(_setting, currency);
                var result = await QuerySettleBetHistoryAsync<T>(_config, batchTime);
                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                await Task.WhenAll(tasks);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TCG GetWagersAsync Exception. batchTime:{batchTime} | currency:{currency}");
                return false;
            }

        }

        private async Task<List<T>> QuerySettleBetHistoryAsync<T>(TCGConfig config, DateTimeOffset batchTime)
        {           
            var result = new List<T>();
            var reportType = config.WagerTypeList.Split(",").Where(i => i != string.Empty);
            foreach (var type in reportType)
            {           
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    var fileUrl = $"/{type}/SETTLED/{batchTime:yyyyMMdd}/{batchTime:yyyyMMddHHmm}_{page:0000}.json";;
                    var queryResult = DownloadFTPFile<T>(config, fileUrl);
                    if (queryResult.list.Count > 0)
                    {
                        result.AddRange(queryResult.list);
                    }         
                    page++;
                    totalPage = 1 + (queryResult.page.total / queryResult.page.pageSize);
                }
            }
            return result;
        }

        public TCGQueryWagerObj<T> DownloadFTPFile<T>(TCGConfig config, string fileUrl)
        {
            try
            {

                if (_configuration.GetValue<string>("ProxyHost") != null)
                {
                    var proxy = new ProxyInfo()
                    {
                        Host = _configuration.GetValue<string>("ProxyHost"),
                        Port = _configuration.GetValue<int>("ProxyPort")
                    };
                    using FtpClient client = new FtpClientHttp11Proxy(proxy);
                    client.Host = config.WagerUrl;
                    client.Credentials = new NetworkCredential(config.WagerAccount, config.WagerPassword);
                    client.AutoDetect();
                    client.Connect();
                    var result = client.DownloadAsync(fileUrl, 0);
                    using Stream stream = new MemoryStream(result.Result);
                    using var sr = new StreamReader(stream);
                    using JsonReader reader = new JsonTextReader(sr);
                    var serializer = new JsonSerializer();
                    _logger.LogInformation($"TCG: {config.WagerAccount} || FTP Success:{config.WagerUrl}{fileUrl} {result.Status} ({Buffer.ByteLength(result.Result)} bytes)");
                    return serializer.Deserialize<TCGQueryWagerObj<T>>(reader);
                }
                else
                {
                    using var client = new FtpClient(config.WagerUrl, config.WagerAccount, config.WagerPassword);
                    client.AutoDetect();
                    client.Connect();
                    var result = client.DownloadAsync(fileUrl, 0);
                    using Stream stream = new MemoryStream(result.Result);
                    using var sr = new StreamReader(stream);
                    using JsonReader reader = new JsonTextReader(sr);
                    var serializer = new JsonSerializer();
                    _logger.LogInformation($"TCG: {config.WagerAccount} || FTP Success:{config.WagerUrl}{fileUrl} {result.Status} ({Buffer.ByteLength(result.Result)} bytes)");
                    return serializer.Deserialize<TCGQueryWagerObj<T>>(reader);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TCG: {config.WagerAccount} || FTP Fail: {config.WagerUrl}{fileUrl} {ex.Message}.");
                return new TCGQueryWagerObj<T>() ;
            }
        }

        private Task<int> InsertWager<T>(T wager)
        {
            var spName = typeof(T) == typeof(TCGSEAWagerData) ? "Service_TpWagerTCG_SEA_Ins" : "Service_TpWagerTCG_VN_Ins";
            return _wagerDbClient.ExecuteAsync(spName, new DynamicParameters(wager));
        }
    }
}

