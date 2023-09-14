using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class FaChaiController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
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
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            logger = provider.GetRequiredService<ILogger<FaChaiController>>();
            thisTp = Tp.FaChai;
            environment = provider.GetRequiredService<IHostEnvironment>();
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(((int)ThirdParty).ToString());
        }
        #endregion
        public class FCRequest
        {
            public string AgentCode { get; set; }
            public string Currency { get; set; }
            public string Params { get; set; }
            public string Sign { get; set; }
        }

        public class FCResponse
        {
            public int Result { get; set; }
            public double MainPoints { get; set; }
            public string ErrorText { get; set; }
        }

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
            var tpCustBalance = tpCustInfoBLL.GetBalance(Convert.ToInt32(siteConfig.Keys), getBalanceReq.MemberAccount, gameType);

            return new FCResponse()
            {
                Result = 0,
                MainPoints = decimal.ToSingle(tpCustBalance.Balance),
            };
        }

        [Route("Fachai/betinfo")]
        [HttpPost]

        public FCResponse Debit(FCRequest req)
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

            var getDebitData = JsonConvert.DeserializeObject<GetDebitData>(para);

            var tpCustBalance = tpCustInfoBLL.GetBalance(Convert.ToInt32(siteConfig.Keys), getDebitData.MemberAccount, gameType);

            if(tpCustBalance.Balance < getDebitData.RequireAmt)
            {
                return new FCResponse()
                {
                    Result = 203
                };
            }
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
