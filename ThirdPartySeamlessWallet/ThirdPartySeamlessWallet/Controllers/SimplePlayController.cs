using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Produces("application/xml")]
    public class SimplePlayController : ControllerBase
    {
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<SimplePlayController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly int _thisTp;
        private readonly int _gameType = 0;
        private readonly string _tpName = Tp.SimplePlay.ToString();
        private readonly SimplePlayConfig _siteConfig;
        private readonly Dictionary<string, SimplePlayConfig> _siteConfigDic;

        public SimplePlayController(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<SimplePlayController>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = (int)Tp.SimplePlay;
            _gameType = (int)GameType.SimplePlay_RNG;
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<SimplePlayConfig>(_tpName);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<SimplePlayConfig>(_tpName);
        }

        #region Request

        #endregion

        #region Response

        public class CommonResp
        {
            public CommonResp()
            {
                error = (int)SimplePlayErrorCode.SystemError;
            }
            public string username { get; set; }
            public string currency { get; set; }
            public decimal amount { get; set; }
            public int error { get; set; }
        }

        #endregion

        [HttpPost]
        [Route("[controller]/GetUserBalance")]
        [Route("[controller]/{sitename}/GetUserBalance")]
        public async Task<CommonResp> GetUserBalanceAsync(string sitename = "")
        {
            var resp = new CommonResp();
            try
            {
                var Content = await GetRequestParamAsync();
                var getReq = JsonConvert.DeserializeObject<BalanceData_SimplePlay>(JsonConvert.SerializeObject(Content));

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, getReq.username);
                if (tpCheck == null)
                {
                    resp.error = (int)SimplePlayErrorCode.SessionExpird;
                    return resp;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = (int)SimplePlayErrorCode.CustLocked;
                    return resp;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, (int)_gameType);
                if (tpCustBalance.TpCurrencyId != getReq.currency)
                {
                    resp.error = (int)SimplePlayErrorCode.CurrencyInvaild;
                    return resp;
                }

                resp.username = getReq.username;
                resp.currency = tpCustBalance.TpCurrencyId;
                resp.amount = tpCustBalance.Balance;
                resp.error = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return resp;
        }

        [HttpPost]
        [Route("[controller]/PlaceBet")]
        [Route("[controller]/{sitename}/PlaceBet")]
        public async Task<CommonResp> PlaceBetAsync(string sitename = "")
        {
            string logName = $"{_tpName}_PlaceBet";
            /*
             当会员在一个游戏内进行投注， SimplePlay 平台会发送这个请求到第三方系统。
             这个请求必须在3秒内回覆，否则我方判定为逾时而取消该项投注，同时也会发送 PlaceBetCancel 到第三方系统，以便第三方系统进行点数回复处理。
            */
            var resp = new CommonResp();
            try
            {
                var Content = await GetRequestParamAsync();
                var getReq = JsonConvert.DeserializeObject<BettingData_SimplePlay>(JsonConvert.SerializeObject(Content));

                // 檢查系統長度的單號
                if (!CheckTransIdLength(getReq.TransId))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"TransId length error {getReq.TransId}", logName);
                    return resp;
                }

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, getReq.username);
                if (tpCheck == null)
                {
                    resp.error = (int)SimplePlayErrorCode.SessionExpird;
                    return resp;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = (int)SimplePlayErrorCode.CustLocked;
                    return resp;
                }

                if (!string.Equals(getReq.gametype, "slot", StringComparison.OrdinalIgnoreCase))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"gametype forbidden {getReq.gametype}", logName);
                    return resp;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                if (tpCustBalance.TpCurrencyId != getReq.currency)
                {
                    resp.error = (int)SimplePlayErrorCode.CurrencyInvaild;
                    return resp;
                }

                if (getReq.amount <= 0)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = tpCustBalance.TpCurrencyId;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.AmountInvaild;
                    return resp;
                }

                var result = _bettingBLL.PlaceBetRNG_SimplePlay(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0)
                {
                    resp.error = (int)SimplePlayErrorCode.Success;
                    resp.currency = getReq.currency;
                    resp.amount = tpAffterAmount;
                    resp.username = getReq.username;
                }
                else if (tpAffterAmount < 0)
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = getReq.currency;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.BalanceNotEnough;
                    _log2File.Error($"insufficient amount {tpAffterAmount}", logName);
                    return resp;
                }
                else
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"unknow error {result}", logName);
                    return resp;
                }

            }
            catch (Exception ex)
            {
                resp.error = (int)SimplePlayErrorCode.SystemError;
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return resp;
        }

        [HttpPost]
        [Route("[controller]/PlayerWin")]
        [Route("[controller]/{sitename}/PlayerWin")]
        public async Task<CommonResp> PlayerWinAsync(string sitename = "")
        {
            string logName = $"{_tpName}_PlayerWin";
            /*
             一般来说，PlaceBet 及 PlayerWin/PlayerLost 在老虎机游戏及捕鱼游戏中，以一对的方式出现。但是请留意， 捕鱼游戏会有例外。
             会员在一局鱼乐无穷中得到了免费子弹，在没有完全使用下退出游戏，当他再进入鱼乐无穷，如果未有转换金币而发射了免费子弹，就这样退出游戏，我方会发出 PlayerWin 或 PlayerLost。
             这样就会发生，在没有 PlaceBet 情况下，收到 PlayerWin 或 PlayerLost 的情况。
             如果第三方有核对 PlayerWin/PlayerLost 的 GameID 能否配上 PlaceBet 才处理的话，就可能导致 PlayerWin/PlayerLost 失败，会员金额无法归还。
             第三方必须理解此一特别情况，而对PlayerWin/PlayerLost 作出适当的处理。
            */
            /*
             当会员有投注过的一局游戏完结及派彩，而会员有净赢额，PlayerWin就会发到第三方系统。
             第三方系统需要修改会员帐户结余。此请求会不停发出，直到被处理及正确回覆为止。
            */
            var resp = new CommonResp();
            try
            {
                var Content = await GetRequestParamAsync();
                var getReq = JsonConvert.DeserializeObject<PayoutData_SimplePlay>(JsonConvert.SerializeObject(Content));

                // 檢查系統長度的單號
                if (!CheckTransIdLength(getReq.TransId))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"TransId length error {getReq.TransId}", logName);
                    return resp;
                }

                if (!string.Equals(getReq.gametype, "slot", StringComparison.OrdinalIgnoreCase))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"gametype forbidden {getReq.gametype}", logName);
                    return resp;
                }

                if (getReq.amount <= 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = tpCustBalance.TpCurrencyId;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.AmountInvaild;
                    return resp;
                }

                getReq.PayoutType = (int)TicketStatus.Win;
                var result = _settledBLL.PayoutRNG_SimplePlay(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    resp.error = (int)SimplePlayErrorCode.Success;
                    resp.currency = getReq.currency;
                    resp.amount = tpAffterAmount;
                    resp.username = getReq.username;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = getReq.currency;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.BalanceNotEnough;
                    _log2File.Error($"insufficient amount {tpAffterAmount}", logName);
                    return resp;
                }
                else
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"unknow error {result}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                resp.error = (int)SimplePlayErrorCode.SystemError;
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return resp;
        }

        [HttpPost]
        [Route("[controller]/PlayerLost")]
        [Route("[controller]/{sitename}/PlayerLost")]
        public async Task<CommonResp> PlayerLostAsync(string sitename = "")
        {
            string logName = $"{_tpName}_PlayerLost";
            /*
             一般来说，PlaceBet 及 PlayerWin/PlayerLost 在老虎机游戏及捕鱼游戏中，以一对的方式出现。但是请留意， 捕鱼游戏会有例外。
            会员在一局鱼乐无穷中得到了免费子弹，在没有完全使用下退出游戏，当他再进入鱼乐无穷，如果未有转换金币而发射了免费子弹，就这样退出游戏，我方会发出 PlayerWin 或 PlayerLost。这样就会发生，在没有 PlaceBet 情况下，收到 PlayerWin 或 PlayerLost 的情况。
            如果第三方有核对 PlayerWin/PlayerLost 的 GameID 能否配上 PlaceBet 才处理的话，就可能导致 PlayerWin/PlayerLost 失败，会员金额无法归还。
            第三方必须理解此一特别情况，而对PlayerWin/PlayerLost 作出适当的处理。
            */
            /*
             当会员有投注过的一局游戏完结及派彩，而会员没有赢额，PlayerLost就会发到第三方系统。
             第三方系统原则上不需要修改会员帐户结余。此请求会不停发出，直到被处理及正确回覆为止。
            */
            var resp = new CommonResp();
            try
            {
                var Content = await GetRequestParamAsync();
                var getReq = JsonConvert.DeserializeObject<PayoutData_SimplePlay>(JsonConvert.SerializeObject(Content));

                // 檢查系統長度的單號
                if (!CheckTransIdLength(getReq.TransId))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"TransId length error {getReq.TransId}", logName);
                    return resp;
                }

                if (!string.Equals(getReq.gametype, "slot", StringComparison.OrdinalIgnoreCase))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"gametype forbidden {getReq.gametype}", logName);
                    return resp;
                }

                getReq.PayoutType = (int)TicketStatus.Lost;
                var result = _settledBLL.PayoutRNG_SimplePlay(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    resp.error = (int)SimplePlayErrorCode.Success;
                    resp.currency = getReq.currency;
                    resp.amount = tpAffterAmount;
                    resp.username = getReq.username;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = getReq.currency;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.BalanceNotEnough;
                    _log2File.Error($"insufficient amount {tpAffterAmount}", logName);
                    return resp;
                }
                else
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"unknow error {result}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                resp.error = (int)SimplePlayErrorCode.SystemError;
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return resp;
        }

        [HttpPost]
        [Route("[controller]/PlaceBetCancel")]
        [Route("[controller]/{sitename}/PlaceBetCancel")]
        public async Task<CommonResp> PlaceBetCancelAsync(string sitename = "")
        {
            string logName = $"{_tpName}_PlaceBetCancel";
            /*
             当发出了的PlaceBet请求逾时或者第三方系统返回错误讯息，我方就会发出PlaceBetCancel请求。 
             SimplePlay 平台会视此次投注失败及通知在线会员相关讯息。第三方系统必须回应此请求及只处理一次。
             如果收到此请求时，没有需要返回点数给会员，即之前没有成功处理PlaceBet，即管如此，第三方平台也必须回覆一个没有错误的返回参数，否则我方平台会不停请求，直到成功为止。
            */
            var resp = new CommonResp();
            try
            {
                var Content = await GetRequestParamAsync();
                var getReq = JsonConvert.DeserializeObject<CancelBetData_SimplePlay>(JsonConvert.SerializeObject(Content));

                if (!string.Equals(getReq.gametype, "slot", StringComparison.OrdinalIgnoreCase))
                {
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"gametype forbidden {getReq.gametype}", logName);
                    return resp;
                }

                if (getReq.amount <= 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = tpCustBalance.TpCurrencyId;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.AmountInvaild;
                    return resp;
                }

                // 超出系統長度的單號，回傳成功避免對方重複CALL
                if (!CheckTransIdLength(getReq.TransId))
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.error = (int)SimplePlayErrorCode.Success;
                    resp.currency = getReq.currency;
                    resp.amount = tpCustBalance.Balance;
                    resp.username = getReq.username;
                    _log2File.Error($"TransId length error {getReq.TransId}", logName);
                    return resp;
                }

                var result = _settledBLL.CancelBetRNG_SimplePlay(getReq, _gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    resp.error = (int)SimplePlayErrorCode.Success;
                    resp.currency = getReq.currency;
                    resp.amount = tpAffterAmount;
                    resp.username = getReq.username;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.currency = getReq.currency;
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.BalanceNotEnough;
                    _log2File.Error($"insufficient amount {tpAffterAmount}", logName);
                    return resp;
                }
                else
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, getReq.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)SimplePlayErrorCode.Error;
                    _log2File.Error($"unknow error {result}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                resp.error = (int)SimplePlayErrorCode.SystemError;
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return resp;
        }

        private async Task<Dictionary<string, string>> GetRequestParamAsync(string sitename = "")
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            var content = HttpUtility.UrlDecode(Content);

            var config = sitename == "" ? _siteConfig : _siteConfigDic[sitename];
            var uriQuery = _tools.DESDecrypt(content, config.EncryptKey);
            Dictionary<string, string> p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
            _log2File.Info($"{JsonConvert.SerializeObject(p)}", "SimplePlay_RequsetDecrypt");
            return p;
        }

        private static bool CheckTransIdLength(string TransId)
        {
            return TransId.Length > 50 ? false : true;
        }
    }
}
