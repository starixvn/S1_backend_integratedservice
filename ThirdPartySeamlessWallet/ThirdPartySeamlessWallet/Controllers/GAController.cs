using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using Org.BouncyCastle.Crypto.Agreement.JPake;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class GAController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly Tp _thisTp;
        private readonly Log2File _log2File;
        private readonly Dictionary<int, GAConfig> _siteConfigDic;
        private readonly IGABLL _gABLL;
        public GAController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = Tp.GA;
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GAConfig>(Tp.GA.ToString());
            _gABLL = provider.GetRequiredService<IGABLL>();
        }
        public class GetBalanceReq
        {
            public string member_ref_id { get; set; }
        }
        public class GetBalanceResp
        {
            public decimal balance { get; set; }
        }
        [HttpGet]
        [Route("[controller]/balance")]
        public IActionResult GetBalance(GetBalanceReq req)
        {
            string logName = $"{_thisTp}_GetBalance";

            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.member_ref_id);
                var thirdPartySetting = _siteConfigDic[tpCustInfo.SiteId];

                Request.Headers.TryGetValue("Authorization", out var authorization);

                if (thirdPartySetting.Token != authorization)
                {
                    _log2File.Error($"Authorization not correct", logName);
                    return BadRequest();
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, req.member_ref_id, (int)GameType.GA_CockFighting);
                GetBalanceResp result = new GetBalanceResp() { balance = tpCustBalance.Balance };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _log2File.Error($"GetBalance\r\n{ex.ToString()}", logName);
                return BadRequest();
            }
        }
        public enum Type
        {
            Bet = 0,
            TicketWinnings = 1,
            TicketLoss = 2
        }

        [HttpPost]
        [Route("[controller]/transfer")]
        public IActionResult Transfer(GATransferReq req)
        {
            string logName = $"{_thisTp}_Transfer";

            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.member_ref_id);
                var thirdPartySetting = _siteConfigDic[tpCustInfo.SiteId];

                Request.Headers.TryGetValue("Authorization", out var authorization);

                if (thirdPartySetting.Token != authorization)
                {
                    _log2File.Error($"Authorization not correct", logName);
                    return BadRequest();
                }

                return req.type switch
                {
                    (int)Type.Bet => _gABLL.Bet(tpCustInfo, req),
                    (int)Type.TicketWinnings => Transaction(tpUserName, req),
                    (int)Type.TicketLoss => Rollback(tpUserName, req),
                    _ => GetBalance(tpUserName, req)
                };
            }
            catch (Exception ex)
            {
                _log2File.Error($"Transfer\r\n{ex.ToString()}", logName);
                return BadRequest();
            }
        }

        private IActionResult Bet(TransferReq req)
        {

        }
    }
}
