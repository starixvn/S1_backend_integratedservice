using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Security.Cryptography;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;
using Newtonsoft.Json;
using System.Text;

namespace ThirdParty.Marbles
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, MarblesConfig> _siteConfig;
        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MarblesConfig>(TpEnum.Marbles.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            var config = _siteConfig[tpCustInfo.SiteId];
            string apiUrl = config.ApiUrl;
            var token = ApplyToken(tpCustInfo);

            LoginRequest request = new()
            {
                handle = "305",
                lineCode = config.MerchantCode,
                agent = config.AgentId,
                account = tpCustInfo.TpUserName,
                ip = inputs["ClientIP"],
                lang = TpLangId,
                playerSession = token,
                currency = tpCustInfo.TpCurrencyId
            };
            var sParam = encodeAESMD5(System.Text.Json.JsonSerializer.Serialize(request), config.AgentKey);

            var result = HttpPost($"{apiUrl}/gapi/api/game/", $"{{\"params\":\"{sParam}\"}}", "application/json");

            var tpData = JsonConvert.DeserializeObject<LoginResponse>(result);

            if (tpData.code == "0")
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = tpData.data.url;
            }
            else
            {
                oApiResult.Message = tpData.msg;
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }
        
        #region UnUse
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
        #region Private
        
        private string encodeAESMD5(string jsonStr, string Key)
        {
            byte[] keyArray = getMd5(Key);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(jsonStr);
            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7; ICryptoTransform cTransform =
            rDel.CreateEncryptor();
            byte[] resultArray =
            cTransform.TransformFinalBlock(toEncryptArray, 0,
            toEncryptArray.Length);
            return byteToHexStr(resultArray);
        }
        private string byteToHexStr(byte[] bytes)
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
        
        #endregion
        
    }
}