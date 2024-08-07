using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto.Tls;
using System.Security.Principal;
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
        [Route("[controller]/api/verifySession")]
        public VerifyResponse Verify(VerifyRequest request)
        {
            if (!_authBLL.AuthenticateByTpToken(request.playerSession, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new VerifyResponse()
                {
                    code = "0",
                    msg = "Success",
                    data = new VerifyResponseData()
                    {
                        traceId = request.traceId,
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
    }
}
