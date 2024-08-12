using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    public class MarblesController : ControllerBase
    {
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly AuthBLL _authBLL;
        private readonly Tp _thisTp;
        private readonly int _gameType;
        private readonly Log2File _log2File;
        private readonly Dictionary<int, MarblesConfig> _siteConfigDic;
        public MarblesController(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = Tp.Marbles;
            _gameType = (int)GameType.Marbles_Lottery;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MarblesConfig>(Tp.Marbles.ToString());
        }
        public class MarblesConfig : TpSiteConfig
        {
            public string MerchantId { get; set; }
            public string MerchantCode { get; set; }
            public string AgentKey { get; set; }
        }
        public class MarblesRequest
        {
            [JsonProperty("params")]
            public string Params { get; set; }
        }
        public class VerifyRequest
        {
            public string traceId { get; set; }
            public string playerSession { get; set; }
        }
        public class VerifyResponse
        {
            public string code { get; set; }
            public string msg { get; set; }
            public VerifyResponseData data { get; set; }
        }
        public class VerifyResponseData
        {
            public string traceId { get; set; }
            public string money { get; set; }
            public string account { get; set; }
        }
        [HttpPost]
        [Route("MBS/verifySession")]
        public VerifyResponse Verify([FromForm] Dictionary<string, string> request)
        {
            var config = _siteConfigDic[1];
            if (!request.ContainsKey("params"))
            {
                return new VerifyResponse()
                {
                    code = "-1",
                    msg = "Fail",
                };
            }

            MarblesRequest marblesRequest = JsonConvert.DeserializeObject<MarblesRequest>(JsonConvert.SerializeObject(request));
            var verifyJson = decodeAESMD5(marblesRequest.Params, config.AgentKey);
            var verifyData = JsonConvert.DeserializeObject<VerifyRequest>(verifyJson);
            if (_authBLL.AuthenticateByTpToken(verifyData.playerSession, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new VerifyResponse()
                {
                    code = "0",
                    msg = "Success",
                    data = new VerifyResponseData()
                    {
                        traceId = verifyData.traceId,
                        money = _tpCustInfoBLL.GetBalance((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType).ToString(),
                        account = tpCustInfoAuth.TpUserName
                    }
                };
            }
            else
            {
                return new VerifyResponse()
                {
                    code = "-1",
                    msg = "Fail",
                };
            }
        }

        private string decodeAESMD5(string encStr, string Key)
        {
            byte[] keyArray = getMd5(Key);
            byte[] toEncryptArray = hexStrToBytes(encStr);
            RijndaelManaged rDel = new RijndaelManaged(); rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray =
            cTransform.TransformFinalBlock(toEncryptArray, 0,
            toEncryptArray.Length);
            return UTF8Encoding.UTF8.GetString(resultArray);

        }
        private byte[] hexStrToBytes(string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];
            for (var x = 0; x < bytes.Length; x++)
            {
                var i = Convert.ToInt32(hexString.Substring(x * 2, 2), 16);
                bytes[x] = (byte)i;
            }
            return bytes;
        }
        private byte[] getMd5(String keyString)
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
    }
}
