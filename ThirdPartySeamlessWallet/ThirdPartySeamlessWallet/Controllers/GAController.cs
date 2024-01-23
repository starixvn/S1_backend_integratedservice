using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

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
        public class GetBalanceResp
        {
            public string balance { get; set; }
        }
        [HttpGet]
        [Route("[controller]/balance")]
        public IActionResult GetBalance(string member_ref_id)
        {
            string logName = $"{_thisTp}_GetBalance";

            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, member_ref_id);

                if(tpCustInfo == null)
                {
                    _log2File.Error($"tpCustInfo is null", logName);
                    return BadRequest();
                }

                var thirdPartySetting = _siteConfigDic[tpCustInfo.SiteId];

                Request.Headers.TryGetValue("Authorization", out var authorization);

                if (thirdPartySetting.Token != authorization)
                {
                    _log2File.Error($"Authorization not correct", logName);
                    return BadRequest();
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, member_ref_id, (int)GameType.GA_CockFighting);
                var result = new GetBalanceResp() { balance = tpCustBalance.Balance.ToString() };
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
            TicketLoss = 2,
            TicketDraw = 3,
            TicketCancelation = 4,
            TicketRollback = 6,
            Void = 9,
            Adjustment = 10,
            Unsettle = 11,
            Uncancel = 12,
            Jackpot = 13
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

                var result = req.type switch
                {
                    (int)Type.Bet => _gABLL.Bet(tpCustInfo, req),
                    (int)Type.TicketWinnings => _gABLL.Payout(tpCustInfo, req),
                    (int)Type.TicketLoss => _gABLL.Payout(tpCustInfo, req),
                    (int)Type.TicketDraw => _gABLL.Payout(tpCustInfo, req),
                    (int)Type.Uncancel => _gABLL.Bet(tpCustInfo, req),
                    (int)Type.Void => _gABLL.RollBack(tpCustInfo, req),
                    (int)Type.TicketRollback => _gABLL.RollBack(tpCustInfo, req),
                    (int)Type.TicketCancelation => _gABLL.RollBack(tpCustInfo, req),
                    _ => (int)GATransactionStatus.Fail
                };

                return result == (int)GATransactionStatus.Success ? Ok() : BadRequest();
            }
            catch (Exception ex)
            {
                _log2File.Error($"Transfer\r\n{ex.ToString()}", logName);
                return BadRequest();
            }
        }
    }
}
