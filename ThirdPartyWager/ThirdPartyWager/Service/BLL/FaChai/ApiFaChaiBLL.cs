using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using Ubiety.Dns.Core;

namespace ThirdPartyWager.Service.BLL.FaChai
{
    public class ApiFaChaiBLL
    {
        private readonly ILogger<ApiFaChaiBLL> _logger;
        private readonly Common _common;
        public ApiFaChaiBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ApiFaChaiBLL>>();
            _common = provider.GetRequiredService<Common>();
        }


        public async Task<RecordListData> GetRecordListasync(Dictionary<string, string> setting, string startTime, string endTime, string currency)
        {
            var request = new GetRecordListRequest()
            {
                StartDate = startTime,
                EndDate = endTime
            };

            var key = setting["Key"];
            var para = JsonConvert.SerializeObject(request);
            var Params = AesEncrypt(para, key);
            var Sign = _common.ToMD5(para).ToLower();

            var dicP = new Dictionary<string, string>();
            dicP.Add("AgentCode", setting["AgentCode"]);
            dicP.Add("Currency", currency);
            dicP.Add("Params", Params);
            dicP.Add("Sign", Sign);

            var result = await HttpPostWithX509(
                $"{setting["ApiUrl"]}/GetRecordList",
                dicP,
                "application/x-www-form-urlencoded"
            );

            var tpData = JsonConvert.DeserializeObject<RecordListData>(result);

            return tpData;
        }
        private string AesEncrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);
            System.Security.Cryptography.RijndaelManaged rm = new
            System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7,
            };
            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateEncryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        private async Task<string> HttpPostWithX509(string url, Dictionary<string, string> parameters, string contentType, Dictionary<string, string> headers = null)
        {
            using var handler = new HttpClientHandler();

            //ignore Certificate
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };

            using var client = new HttpClient(handler);
            try
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, item.Value ?? string.Empty);
                    }
                }
                var formData = new FormUrlEncodedContent(parameters);
                var response = await client.PostAsync(url, formData);
                var sb = response.Content.ReadAsStringAsync().Result;
                _logger.LogInformation($"response: {sb}");
                return sb;
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    _logger.LogError(ex, ex.Message);
                }
                return ae.Message;
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex, ex.InnerException.Message);
                }
                return ex.Message;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                return e.Message;
            }
        }
    }
}
