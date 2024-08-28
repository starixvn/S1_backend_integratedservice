using Newtonsoft.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDragonGamingBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.WonCasino.ToString();
        public ApiDragonGamingBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public async Task<WagerWonCasinoModel> GetWagerAsync(WonCasinoCurrency currencySetting, string apiUrl, long startTime, long endTime, int index)
        {
            var tmpUrl = $"{apiUrl}/api/record/bets/detail";
            SortedDictionary<string, string> paraDic = new SortedDictionary<string, string>();
            paraDic.Add("appid", currencySetting.AppId);
            paraDic.Add("index", index.ToString());
            paraDic.Add("size", "2000");
            paraDic.Add("begintime", startTime.ToString());
            paraDic.Add("endtime", endTime.ToString());
            
            
            _log2File.InsertRequestLog(_tpName, tmpUrl, $"{JsonConvert.SerializeObject(paraDic)}", "WinCasino encryption request");
            var sign = GetSignature(paraDic, currencySetting.SecretKey);
            List<string> paras = new List<string>();
            foreach (KeyValuePair<string, string> kvp in paraDic)
            {
                string key = kvp.Key.ToLower();
                string value = kvp.Value;
                paras.Add(key + "=" + value);
            }
            string param = string.Join("&", paras.ToArray()) + "&sign=" + sign;
            var url = $"{tmpUrl}?{param}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            _log2File.InsertRequestLog(_tpName, apiUrl, $"{url}", "request Url");
            var response = await _httpHelper.GetClient().SendAsync(request);
            var r = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, r);
                return null;
            }

            var result = JsonConvert.DeserializeObject<WagerWonCasinoModel>(r);
            return result;
        }

        private string GetSignature(SortedDictionary<string, string> geek, string secretKey)
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, string> kvp in geek)
            {
                string key = kvp.Key.ToLower();
                string value = kvp.Value;
                list.Add(key + "=" + value);
            }
            string raw_data = string.Join("&", list.ToArray()) + "&key=" + secretKey;
            using (MD5 md5 = MD5.Create())
            {
                byte[] rawBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(raw_data));
                return BitConverter.ToString(rawBytes).Replace("-", "").ToLower();
            }
        }
    }
}
