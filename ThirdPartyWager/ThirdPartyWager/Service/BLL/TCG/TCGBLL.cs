using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;

using Dapper;

using Newtonsoft.Json;

using Serilog.Context;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class TCGBLL
    {
        private readonly ILogger<TCGBLL> _logger;
        private readonly MySqlClient _wagerDbClient;
        private readonly Tp _tp = Tp.TCG;
        private readonly Dictionary<string, string> _setting;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private const int ErrorCount = 3; //容許錯誤次數
        private readonly ConcurrentDictionary<string, int> _errorCounter = new();
        private readonly CommonBLL _commonBLL;

        public TCGBLL(IServiceProvider provider)
        {
            LogContext.PushProperty("Tp", _tp.ToString());
            _logger = provider.GetRequiredService<ILogger<TCGBLL>>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);

            //用 SiteId = 0 取回預設Config
            _setting = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(_tp.ToString());

            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _commonBLL = provider.GetRequiredService<CommonBLL>();
        }

        public async Task<bool> GetWagersAsync<T>(DateTimeOffset batchTime, string currency, WagerTimerType timerType)
        {
            try
            {
                var config = new TCGConfig(_setting, currency);
                var result = await QuerySettleBetHistoryAsync<T>(config, batchTime, timerType);
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

        public async Task<bool> GetUnsettleAsync<T>(DateTimeOffset batchTime, string currency)
        {
            try
            {
                var config = new TCGConfig(_setting, currency);
                var result = await GetUnsettleData<T>(config, batchTime, currency);
                var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                await Task.WhenAll(tasks);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TCG GetUnsettleAsync Exception. batchTime:{batchTime} | currency:{currency} | {ex.Message}");
                return false;
            }
        }

        private Task<List<T>> QuerySettleBetHistoryAsync<T>(TCGConfig config, DateTimeOffset batchTime, WagerTimerType timerType)
        {
            var result = new List<T>();
            var reportType = config.WagerTypeList.Split(",").Where(i => i != string.Empty);
            foreach (var type in reportType)
            {
                var page = 1;
                var totalPage = 1;
                while (page <= totalPage)
                {
                    var fileUrl = $"{config.WagerUrl}{type}/SETTLED/{batchTime:yyyyMMdd}/{batchTime:yyyyMMddHHmm}_{page:0000}.json";
                    var queryReuslt = DownloadFTPFile<T>(config, fileUrl);
                    if (queryReuslt.Pass) // 回傳Pass = True先略過此次時間
                    {
                        //pass先寫入DB的Redo Table，用redo的worker另外處理
                        if (timerType == WagerTimerType.Wager)
                        {
                            _commonBLL.InsertTpWagerRedo(new TpWagerRedo()
                            {
                                TpId = (int)_tp,
                                Time = batchTime.DateTime,
                                Parameter = JsonConvert.SerializeObject(new TCGRedoObj
                                {
                                    TpCurrencyId = config.TpCurrencyId
                                })
                            });
                        }
                        return Task.FromResult(new List<T>());
                    }

                    if (queryReuslt.list.Count > 0)
                    {
                        result.AddRange(queryReuslt.list);
                    }
                    page++;
                    totalPage = 1 + (queryReuslt.page.total / queryReuslt.page.pageSize);
                }
            }
            return Task.FromResult(result);
        }

        private TCGQueryWagerObj<T> DownloadFTPFile<T>(TCGConfig config, string fileUrl)
        {
            try
            {
                var request = (FtpWebRequest)WebRequest.Create(fileUrl);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(config.WagerAccount, config.WagerPassword);
                var response = (FtpWebResponse)request.GetResponse();

                using var s = response.GetResponseStream();
                using var sr = new StreamReader(s);
                using JsonReader reader = new JsonTextReader(sr);
                var serializer = new JsonSerializer();
                _logger.LogInformation($"TCG: {config.WagerAccount} || FTP Success:{response.StatusDescription}");
                return serializer.Deserialize<TCGQueryWagerObj<T>>(reader);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TCG: {config.WagerAccount} || FTP Fail: {fileUrl} {ex.Message}.");
                var pass = FTPErrorHandler(fileUrl);
                if (pass)
                {
                    return new TCGQueryWagerObj<T>()
                    {
                        Pass = true
                    };
                }
                else
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// 計算這筆FTP路徑錯誤次數，超過一定次數會另外存放紀錄，回傳True會略過此次Batch時間
        /// </summary>
        private bool FTPErrorHandler(string fileUrl)
        {
            if (_errorCounter.TryGetValue(fileUrl, out var count))
            {
                var newCount = count + 1;
                if (newCount >= ErrorCount) // 超過容許次數另外存放
                {
                    _errorCounter.Remove(fileUrl, out var _);
                    return true;
                }
                else
                {
                    _errorCounter.TryUpdate(fileUrl, newCount, count);
                }
            }
            else
            {
                _errorCounter.TryAdd(fileUrl, 1);
            }
            return false;
        }

        private Task<int> InsertWager<T>(T wager)
        {
            var spName = typeof(T) == typeof(TCGSEAWagerData) ? "Service_TpWagerTCG_SEA_Ins" : "Service_TpWagerTCG_VN_Ins";
            return _wagerDbClient.ExecuteAsync(spName, new DynamicParameters(wager));
        }

        private async Task<List<T>> GetUnsettleData<T>(TCGConfig config, DateTimeOffset batchTime, string currency)
        {
            var isVN = typeof(T) == typeof(TCGVNWagerData);
            var result = new List<T>();
            var page = 1;
            var totalPage = 1;
            while (page <= totalPage)
            {
                var sParam = new
                {
                    method = isVN ? "gluoh" : "elubd",
                    product_type = isVN ? 420 : 384,
                    batch_name = $"{batchTime:yyyyMMddHHmm}",
                    page
                };
                var httpContent = new FormUrlEncodedContent(GetBody(sParam, config));
                var response = await _httpHelper.GetClient().PostAsync(config.ApiUrl, httpContent);
                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"{currency} || {json}");

                var queryReuslt = _common.FromJson<TCGUnSettleObj>(json);
                if (queryReuslt.status != 0)
                {
                    throw new Exception($"{queryReuslt.status} | {queryReuslt.error_desc}");
                }

                var unSettleData = isVN ?
                    (IEnumerable<T>)queryReuslt.orders.Select(i => new TCGVNWagerData(i)) :
                    (IEnumerable<T>)queryReuslt.details.Select(i => new TCGSEAWagerData(i));

                result.AddRange(unSettleData);
                page = queryReuslt.page_info.currentPage + 1;
                totalPage = queryReuslt.page_info.totalPage;
            }

            return result;
        }

        private Dictionary<string, string> GetBody(object obj, TCGConfig config)
        {
            var dParam = DesEncrypt(_common.ToJson(obj), config.DesKey);
            var sign = SHA256($"{dParam}{config.SignKey}");
            var result = new Dictionary<string, string>
            {
                { "merchant_code", config.MerchantCode },
                { "params", dParam},
                { "sign", sign},
            };
            return result;
        }

        /// <summary>
        /// DES加密
        /// </summary>
        private string DesEncrypt(string input, string key)
        {
            var toEncryptArray = Encoding.UTF8.GetBytes(input);
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using var cTransform = des.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }
    }
}

