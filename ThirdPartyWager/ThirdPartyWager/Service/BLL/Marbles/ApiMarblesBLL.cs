using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiMarblesBLL
    {
        private readonly IHostEnvironment _environment;
        private readonly HttpHelper _httpHelper;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.Marbles.ToString();
        public ApiMarblesBLL(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<MBSHandleResponse> GetMarblesWagerAsync(MarblesConfig setting, long startTime, long endTime, int page = 1)
        {
            var handleApiUrl = $"{setting.ApiUrl}/gapi/api/handle/";
            var startTimeUnix = startTime * 1000;
            var endTimeUnix = endTime * 1000;
            var mode = 1;
            // 1: Dev 2: Prod
            mode = !_environment.IsProduction() ? 2 : 1; 
            
            
            MarblesGetWagerRequest request = new()
            {
                handle = "360",
                lineCode = setting.MerchantCode,
                agent = setting.AgentId,
                startDate = startTimeUnix.ToString(),
                endDate = endTimeUnix.ToString(),
                page = page.ToString(),
                pageSize = 5000.ToString(), // 直接拉最多 TP有建議五分鐘拉一次
                mode = mode.ToString() 
            };
            var sParam = EncodeAESMD5(System.Text.Json.JsonSerializer.Serialize(request), setting.AgentKey);
            _log2File.InsertRequestLog(_tpName, handleApiUrl, $"{JsonConvert.SerializeObject(request)}", "Marbles game encryption request");

            var insertRequest = new
            {
                @params = sParam
            };
            
            var r = await DoHttpClientAsync(handleApiUrl, insertRequest);
            var result = JsonConvert.DeserializeObject<MBSHandleResponse>(r);
            
            return result;
        }

        #region Private
        
        private string EncodeAESMD5(string jsonStr, string Key)
        {
            byte[] keyArray = GetMd5(Key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(jsonStr);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7; ICryptoTransform cTransform =
            rDel.CreateEncryptor();
            byte[] resultArray =
            cTransform.TransformFinalBlock(toEncryptArray, 0,
            toEncryptArray.Length);
            return ByteToHexStr(resultArray);
        }
        private string ByteToHexStr(byte[] bytes)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    returnStr += bytes[i].ToString("X2").ToUpper();
                }
            }
            return returnStr;
        }
        private byte[] GetMd5(String keyString)
        {
            MD5 md5 = MD5.Create();
            byte[] buffer =
            Encoding.GetEncoding("UTF-8").GetBytes(keyString);
            byte[] MD5Buffer = md5.ComputeHash(buffer);
            byte[] rawKey = new byte[32];
            Array.Copy(MD5Buffer, 0, rawKey, 0, 16);
            Array.Copy(MD5Buffer, 0, rawKey, 16, 16);
            return rawKey;
        }
        
        private byte[] HexStrToBytes(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            for (var x = 0; x < bytes.Length; x++)
            {
                var i = Convert.ToInt32(hexString.Substring(x * 2, 2), 16);
                bytes[x] = (byte)i;
            }
            return bytes;
        }
        
        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var json = JsonConvert.SerializeObject(param);
            _log2File.InsertRequestLog(_tpName, url, json, "1");

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;
            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, json, result);
                return result;
            }

            return result;
        }
        
        #endregion
    }
}
