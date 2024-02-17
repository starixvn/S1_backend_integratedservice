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
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly Tp _thisTp;
        private readonly Log2File _log2File;
        private readonly Dictionary<int, GAConfig> _siteConfigDic;
        private readonly IGABLL _gABLL;
        public GAController(IServiceProvider provider)
        {
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
                    (int)GAType.Bet => _gABLL.Bet(tpCustInfo, req),
                    (int)GAType.TicketWinnings => _gABLL.Payout(tpCustInfo, req),
                    (int)GAType.TicketLoss => _gABLL.Payout(tpCustInfo, req),
                    (int)GAType.TicketDraw => _gABLL.Payout(tpCustInfo, req),
                    (int)GAType.Uncancel => _gABLL.Bet(tpCustInfo, req),
                    (int)GAType.Void => _gABLL.RollBack(tpCustInfo, req),
                    (int)GAType.TicketRollback => _gABLL.RollBack(tpCustInfo, req),
                    (int)GAType.TicketCancelation => _gABLL.RollBack(tpCustInfo, req),
                    (int)GAType.Jackpot => _gABLL.Jackpot(tpCustInfo, req),
                    (int)GAType.Unsettle => _gABLL.Unsettle(tpCustInfo, req),
                    _ => (int)GATransactionStatus.Fail
                };

                if(result == (int)GATransactionStatus.Success)
                {
                    _log2File.Info($"Transfer Success", logName);
                }
                else
                {
                    _log2File.Error(((GATransactionStatus)result).ToString(), logName);
                }


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
