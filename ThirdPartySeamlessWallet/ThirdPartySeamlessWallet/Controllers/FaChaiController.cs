using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class FaChaiController : ControllerBase
    {
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IFaChaiBLL _faChakiBLL;
        private readonly Tp thisTp;
        private readonly ILogger<FaChaiController> logger;
        private readonly IHostEnvironment environment;
        private readonly Dictionary<int, Dictionary<string, string>> siteConfigDic;

        public Tp ThirdParty { get; set; }
        public int gameType { get; set; }

        #region Constructors
        public FaChaiController(IServiceProvider provider)
        {
            ThirdParty = Tp.FaChai;
            gameType = (int)GameType.FaChai_RNG;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _faChakiBLL = provider.GetRequiredService<IFaChaiBLL>();
            logger = provider.GetRequiredService<ILogger<FaChaiController>>();
            thisTp = Tp.FaChai;
            environment = provider.GetRequiredService<IHostEnvironment>();
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>((ThirdParty).ToString());
        }
        #endregion

        public class GetBalanceData
        {
            public string MemberAccount { get; set; }
            public string Currency { get; set; }
            public string GameID { get; set; }
            public long Ts { get; set; }
        }
        public class GetDebitData
        {
            public string RecordID { get; set; }
            public string BankID { get; set; }
            public string MemberAccount { get; set; }
            public string Currency { get; set; }
            public int GameID { get; set; }
            public int GameType { get; set; }
            public bool isBuyFeature { get; set; }
            public decimal Bet { get; set; }
            public decimal Win { get; set; }
            public decimal JPBet { get; set; }
            public decimal JPPrize { get; set; }
            public decimal NetWin { get; set; }
            public decimal RequireAmt { get; set; }
            public DateTime GameDate { get; set; }
            public DateTime CreateDate { get; set; }
            public long Ts { get; set; }
        }

        [Route("Fachai/getbalance")]
        [HttpPost]
        public FCResponse GetBalance(FCRequest req)
        {
            var configList = siteConfigDic.Select(x => x.Value).ToList();
            var siteConfig = configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = AesDecrypt(req.Params, siteConfig["Key"]);

            if(para == null)
            {
                return new FCResponse()
                {
                    Result = 604
                };
            }
            var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceData>(para);
            var tpCustBalance = _tpCustInfoBLL.GetBalance(Convert.ToInt32(siteConfig.Keys), getBalanceReq.MemberAccount, gameType);

            return new FCResponse()
            {
                Result = 0,
                MainPoints = decimal.ToSingle(tpCustBalance.Balance),
            };
        }

        [Route("Fachai/betinfo")]
        [HttpPost]

        public FCResponse Bet(FCRequest req)
        {
            var configList = siteConfigDic.Select(x => x.Value).ToList();
            var siteConfig = configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = AesDecrypt(req.Params, siteConfig["Key"]);

            if (para == null)
            {
                return new FCResponse()
                {
                    Result = 604
                };
            }

            var bettingData = JsonConvert.DeserializeObject<BettingDataRNG_FaChai>(para);

            var tpCustBalance = _tpCustInfoBLL.GetBalance(Convert.ToInt32(siteConfig.Keys), bettingData.MemberAccount, gameType);

            if (tpCustBalance.Balance < bettingData.RequireAmt)
            {
                return new FCResponse()
                {
                    Result = 203
                };
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, bettingData.MemberAccount);

            var result = _faChakiBLL.Bet(tpCustInfo, bettingData, out decimal afterAmount);

            if (result > 0)
            {
                return new FCResponse()
                {
                    Result = 0,
                    MainPoints = decimal.ToSingle(afterAmount)
                };
            }
            else
            {
                return new FCResponse()
                {
                    Result = 702,
                };
            }
        }

        [Route("Fachai/cancelbetinfo")]
        [HttpPost]
        public FCResponse Cancel(FCRequest req)
        {
            var configList = siteConfigDic.Select(x => x.Value).ToList();
            var siteConfig = configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = AesDecrypt(req.Params, siteConfig["Key"]);

            if (para == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Verification_failed
                };
            }

            var cancelData = JsonConvert.DeserializeObject<CnacelData_FaChai>(para);

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, cancelData.MemberAccount);

            if (tpCustInfo == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Account_does_not_exist
                };
            }

            var result = _faChakiBLL.Cancel(tpCustInfo, cancelData);

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.FaChai, tpCustInfo.CurrencyId);

            //if (result.Result == 0)
            //{
            //    if (afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            //    {
            //        afterAmount /= tpCurrency.ExchangeRate;
            //    }
            //    else if (afterAmount == 0)
            //    {
            //        afterAmount = _tpCustInfoBLL.GetBalance((int)Tp.Skywind, cancelData.MemberAccount, (int)GameType.FaChai_RNG).Balance;
            //    }

            //    result.MainPoints = decimal.ToSingle(afterAmount);
            //}
            //else
            //{
            //    result.MainPoints = 0;
            //}

            result.MainPoints = 0;

            return result;
        }
        public static string AesDecrypt(string str, string key)
        {
            if (string.IsNullOrEmpty(str)) return null;
            Byte[] toEncryptArray = Convert.FromBase64String(str);
            System.Security.Cryptography.RijndaelManaged rm = new
            System.Security.Cryptography.RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = System.Security.Cryptography.CipherMode.ECB,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7
            };
            System.Security.Cryptography.ICryptoTransform cTransform = rm.CreateDecryptor();
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Encoding.UTF8.GetString(resultArray);
        }

    }
}
