using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;
using Newtonsoft.Json;
using System.Text;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

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
            var sParam = EncodeAESMD5(System.Text.Json.JsonSerializer.Serialize(request), config.AgentKey);

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
        
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //Marbles無注單明細API 拿Bettrans
            var getSpName = "Service_BettransLT_Marbles_Detail_Get";

            TpCurrency tpCurrency = new()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            
            var YYYYMM = getBetDetail.SettledTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery(getSpName, new { YYYYMM, TransId = getBetDetail.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return null;
            }
            
            var getDetail = detailList.LastOrDefault();
            var detail = ToJson(detailList);
            
            return new LotteryDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
                RefTransId = getBetDetail.TransId,
                TpUserName = getDetail.GetValueOrDefault("account")?.ToString(),
                UTCBetTime = DateTime.Parse(getDetail.GetValueOrDefault("startTime")?.ToString()).ToUniversalTime(),
                UTCSettledTime = DateTime.Parse(getDetail.GetValueOrDefault("endTime")?.ToString()).ToUniversalTime(),
                Stake = Math.Abs((decimal)getDetail.GetValueOrDefault("bet")),
                WinlostAmount = (decimal)getDetail.GetValueOrDefault("profit") -  Math.Abs((decimal)getDetail.GetValueOrDefault("bet"))
            };
        }
        
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var YYYYMM = DateTimeOffset.FromUnixTimeSeconds(betRecordInfo.SettledTime).DateTime.GetPartitionKey();
            var detailList = TpBetRecordProvider.DbQuery("Service_BettransLT_Marbles_Detail_Get", new { YYYYMM, TransId = betRecordInfo.TransId }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }
            
            var getDetail = detailList.LastOrDefault();

            var GameId = getDetail.GetValueOrDefault("gameCode").ToString();
            var GameRoundId = getDetail.GetValueOrDefault("gameNo").ToString();

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = GameId,
                Game = GameId,
                RefNo = GameRoundId,
                Title = tpGameType.TypeName,
                MatchID = GameRoundId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty
            };
        }
        
        #region UnUse
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }
        
        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        
        #endregion
        
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
        private static  byte[] GetMd5(String keyString)
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
        private static string DecodeAESMD5(string encStr, string Key)
        {
            byte[] keyArray = GetMd5(Key);
            byte[] toEncryptArray = HexStrToBytes(encStr);
            RijndaelManaged rDel = new RijndaelManaged(); rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray =
            cTransform.TransformFinalBlock(toEncryptArray, 0,
            toEncryptArray.Length);
            return UTF8Encoding.UTF8.GetString(resultArray);

        }
        private static byte[] HexStrToBytes(string hexString)
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