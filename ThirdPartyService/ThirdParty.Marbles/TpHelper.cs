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

        public class MarblesConfig : TpSiteConfig
        {
            public string MerchantId { get; set; }
            public string MerchantCode { get; set; }
            public string AgentKey { get; set; }
            public string AgentId { get; set; }
        }

        public class LoginRequest
        {
            public string handle { get; set; }
            public string lineCode { get; set; }
            public string agent { get; set; }
            public string account { get; set; }
            //public string gameCode { get; set; }
            public string ip { get; set; }
            //public string backUrl { get; set; }
            public string lang { get; set; }
            public string playerSession { get; set; }
            public string currency { get; set; }
            //public string ctype { get; set; }
        }
        public class LoginResponse
        {
            public string code { get; set; }
            public string msg { get; set; }
            public LoginResponseData data { get; set; }
        }
        public class LoginResponseData
        {
            public string url { get; set; }
        }
        public enum MarblesErrorCode
        {
            /// <summary>
            /// 业务异常
            /// </summary>
            E000,

            /// <summary>
            /// 不合法的请求
            /// </summary>
            E010,

            /// <summary>
            /// 不合法的Handle
            /// </summary>
            E011,

            /// <summary>
            /// 参数不合法
            /// </summary>
            E012,

            /// <summary>
            /// 解密异常
            /// </summary>
            E013,

            /// <summary>
            /// 填充参数出错.{0}
            /// </summary>
            E014,

            /// <summary>
            /// handle处理异常
            /// </summary>
            E015,

            /// <summary>
            /// 域名不存在
            /// </summary>
            E016,

            /// <summary>
            /// IP限制,禁止访问
            /// </summary>
            E017,

            /// <summary>
            /// 请求太频繁，请{0}秒后再试！
            /// </summary>
            E018,

            /// <summary>
            /// 密码错误
            /// </summary>
            E019,

            /// <summary>
            /// Token错误
            /// </summary>
            E020,

            /// <summary>
            /// 中心域名不存在
            /// </summary>
            E021,

            /// <summary>
            /// 站点号不存在
            /// </summary>
            E022,

            /// <summary>
            /// 代理号不存在
            /// </summary>
            E023,

            /// <summary>
            /// 帐号已经存在
            /// </summary>
            E031,

            /// <summary>
            /// 帐号不存在
            /// </summary>
            E032,

            /// <summary>
            /// 保存失败
            /// </summary>
            E033,

            /// <summary>
            /// 交易号已存在
            /// </summary>
            E034,

            /// <summary>
            /// 交易号不存在
            /// </summary>
            E035,

            /// <summary>
            /// 交易失败
            /// </summary>
            E036,

            /// <summary>
            /// 余额不足
            /// </summary>
            E037,

            /// <summary>
            /// 帐号已停用(转入，转出交易失败)
            /// </summary>
            E038,

            /// <summary>
            /// 帐号已冻结(转入，转出交易失败)
            /// </summary>
            E039,

            /// <summary>
            /// 帐号未激活/未审核
            /// </summary>
            E040,

            /// <summary>
            /// 帐号审核失败
            /// </summary>
            E041,

            /// <summary>
            /// 当天取款超出限额
            /// </summary>
            E042,

            /// <summary>
            /// 会员在玩游戏，不能做取款操作
            /// </summary>
            E043,

            /// <summary>
            /// 帐号登录错误次数超过{0}次，当天将限制访问
            /// </summary>
            E044,

            /// <summary>
            /// 站长分数不足，请咨询站长
            /// </summary>
            E045,

            /// <summary>
            /// 商户处理失败
            /// </summary>
            ET001,

            /// <summary>
            /// 系统维护中
            /// </summary>
            M001
        }
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
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
                oApiResult.Message = tpData.msg;
            }
            else
            {
                oApiResult.Message = tpData.msg;
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

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
    }
}