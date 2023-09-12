using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
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

        #region Constructors
        public FaChaiController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            logger = provider.GetRequiredService<ILogger<FaChaiController>>();
            thisTp = Tp.FaChai;
            environment = provider.GetRequiredService<IHostEnvironment>();
        }
        #endregion
        public class FCRequest
        {
            public string AgentCode { get; set; }
            public string Currency { get; set; }
            public string Params { get; set; }
            public string Sign { get; set; }
        }

        public class GetBalanceReq
        {
            public string MemberAccount { get; set; }
            public string Currency { get; set; }
            public string GameID { get; set; }
            public long Ts { get; set; }
        }
        public class GetBalanceResp
        {
            public int Result { get; set; }
            public double MainPoints { get; set; }
            public string ErrorText { get; set; }
        }

        [Route("[controller]/getbalance")]
        [HttpPost]
        public GetBalanceResp GetBalance(FCRequest req)
        {
            return null;
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
