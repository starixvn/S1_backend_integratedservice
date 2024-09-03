using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
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
        private readonly Dictionary<int, MarblesConfig> _siteConfigDic;
        private readonly IMBSBLL _IMBSBLL;
        
        public MarblesController(IServiceProvider provider)
        {
            _IMBSBLL = provider.GetRequiredService<IMBSBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thisTp = Tp.Marbles;
            _gameType = (int)GameType.Marbles_Lottery;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MarblesConfig>(Tp.Marbles.ToString());
        }
        
        [HttpPost]
        [Route("MBS/verifySession")]
        public VerifyResponse<VerifyResponseData> Verify([FromForm] Dictionary<string, string> request)
        {
            var config = _siteConfigDic[1];
            if (!request.ContainsKey("params"))
            {
                return new VerifyResponse<VerifyResponseData>
                {
                    code = "-1",
                    msg = "Fail",
                    data = null
                };
            }

            MarblesRequest marblesRequest = JsonConvert.DeserializeObject<MarblesRequest>(JsonConvert.SerializeObject(request));
            var verifyJson = decodeAESMD5(marblesRequest.Params, config.AgentKey);
            var verifyData = JsonConvert.DeserializeObject<VerifyRequest>(verifyJson);
            if (_authBLL.Authenticate(verifyData.playerSession, out TpCustInfoAuth tpCustInfoAuth))
            {
                var resultData = new VerifyResponseData()
                {
                    traceId = verifyData.traceId,
                    money = _tpCustInfoBLL.GetBalance((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType).Balance
                        .ToString(),
                    account = tpCustInfoAuth.TpUserName
                };
                
                return new VerifyResponse<VerifyResponseData>()
                {
                    code = "0",
                    msg = "Success",
                    data = resultData
                };
            }
            else
            {
                return new VerifyResponse<VerifyResponseData>
                {
                    code = "-1",
                    msg = "Fail",
                    data = null,
                };
            }
        }

        [HttpPost]
        [Route("MBS/getBalance")]
        public MBSGetBalanceResponse GetBalance([FromForm] Dictionary<string, string> request)
        {
            var config = _siteConfigDic[1];
            if (!request.ContainsKey("params"))
            {
                return new MBSGetBalanceResponse()
                {
                    code = "-1",
                    msg = "Fail",
                };
            }

            MarblesRequest marblesRequest = JsonConvert.DeserializeObject<MarblesRequest>(JsonConvert.SerializeObject(request));
            var getBalanceJson = decodeAESMD5(marblesRequest.Params, config.AgentKey);
            var getBalanceData = JsonConvert.DeserializeObject<MBSGetBalanceRequest>(getBalanceJson);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, GetTpUserName(getBalanceData.account), _gameType);

            if (tpCustBalance != null)
            {
                return new MBSGetBalanceResponse()
                {
                    code = "0",
                    msg = "Success",
                    data = new MBSGetBalanceResponseData()
                    {
                        traceId = getBalanceData.traceId,
                        money = tpCustBalance.Balance.ToString(),
                        account = getBalanceData.account,
                        currency = tpCustBalance.TpCurrencyId
                    }
                };
            }
            else
            {
                return new MBSGetBalanceResponse()
                {
                    code = "-1",
                    msg = "Fail",
                };
            }
        }

        [HttpPost]
        [Route("MBS/transferInOut")]
        public VerifyResponse<MBSTransferInOutResponse> TransferInOut([FromForm] Dictionary<string, string> request)
        {
            var config = _siteConfigDic[1];
            if (!request.ContainsKey("params"))
            {
                return new VerifyResponse<MBSTransferInOutResponse>()
                {
                    code = "-1",
                    msg = "Fail",
                    data = null
                };
            }

            var result = new MBSTransferInOutResponse();
            
            var marblesRequest = JsonConvert.DeserializeObject<MarblesRequest>(JsonConvert.SerializeObject(request));
            var transferInOutJson = decodeAESMD5(marblesRequest.Params, config.AgentKey);
            var transferInOutData = JsonConvert.DeserializeObject<MBSTransferInOutRequest>(transferInOutJson);

            result = transferInOutData.type switch
            {
                1 => _IMBSBLL.Bet(transferInOutData),
                2 => _IMBSBLL.Settle(transferInOutData),
                _ => result
            };

            if (result.resultBets is { Count: >= 0 })
            {
                return new VerifyResponse<MBSTransferInOutResponse>()
                {
                    code = "0",
                    msg = "Success",
                    data = result
                };
            }

            return new VerifyResponse<MBSTransferInOutResponse>()
            {
                code = "-1",
                msg = "Fail",
                data = null
            };
        }


        #region Private
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
        private string GetTpUserName(string marblesAccount)
        {
            return marblesAccount.Substring(7);
        }
        
        #endregion
    }
}
