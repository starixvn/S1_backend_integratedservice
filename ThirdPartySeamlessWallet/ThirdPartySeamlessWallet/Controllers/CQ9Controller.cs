using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class CQ9Controller : ControllerBase
    {
        private readonly Tools _tools;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly string _tpGameName;
        private readonly IHostEnvironment _environment;
        private readonly Dictionary<string, string> _defaultConfig; // SiteConfig的SiteId=0
        private readonly Dictionary<string, Dictionary<string, string>> _siteNameConfigs;

        public CQ9Controller(IServiceProvider provider)
        {
            _tools = provider.GetRequiredService<Tools>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _thisTp = Tp.CQ9;
            _tpGameName = "CQ9";
            // defaultConfig給原本NOVA、GOAL、BWING的商號使用
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(_thisTp.ToString());
            // 新增商號使用指定siteName
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<Dictionary<string, string>>(_thisTp.ToString());
        }

        public class Payload
        {
            public string currency { get; set; }
            public string account { get; set; }
        }

        public class GetHeader
        {
            [FromHeader]
            public string wtoken { get; set; }
        }

        #region Request
        public class GetBalanceReq
        {
            public string gamecode { get; set; }
        }
        #endregion

        #region Response
        public class CheckPlayerResp
        {
            public bool data { get; set; }
            public GameResponse status { get; set; }
        }

        public class GetApiResp
        {
            public GetApiResp()
            {
                data = new BalanceData();
                status = new GameResponse();
            }
            public BalanceData data { get; set; }
            public GameResponse status { get; set; }
        }

        public class BalanceData
        {
            private decimal _balance = 0m;
            public decimal balance
            {
                get
                {
                    decimal.TryParse(_balance.ToString("#.0000"), out _balance);
                    return _balance;
                }
                set
                {
                    _balance = value;
                }
            }
            public string currency { get; set; }
        }

        public class GameResponse
        {
            public string code { get; set; }
            public string message { get; set; }
            public string datetime { get; set; }
        }

        public class GetRecordResp
        {
            public GetRecordResp()
            {
                data = new RecordData();
                status = new GameResponse();
            }
            public RecordData data { get; set; }
            public GameResponse status { get; set; }
        }

        public class RecordData
        {
            public RecordData()
            {
                target = new Target();
                status = new DetailStatus();
            }
            public string _id { get; set; }
            public string action { get; set; }
            public Target target { get; set; }
            public DetailStatus status { get; set; }
            public decimal before { get; set; }
            private decimal _balance = 0m;
            public decimal balance
            {
                get
                {
                    decimal.TryParse(_balance.ToString("#.0000"), out _balance);
                    return _balance;
                }
                set
                {
                    _balance = value;
                }
            }
            public string currency { get; set; }
            [JsonProperty("event")]
            public List<RecordEvent> Event { get; set; }
        }

        public class Target
        {
            public string account { get; set; }
        }

        public class DetailStatus
        {
            public string createtime { get; set; }
            public string endtime { get; set; }
            public string status { get; set; }
            public string message { get; set; }
        }

        public class RecordEvent
        {
            public string mtcode { get; set; }
            public decimal amount { get; set; }
            public string eventtime { get; set; }
        }

        public class GetTakeAllResp
        {
            public GetTakeAllResp()
            {
                data = new TakeAllData();
                status = new GameResponse();
            }
            public TakeAllData data { get; set; }
            public GameResponse status { get; set; }
        }

        public class TakeAllData
        {
            private decimal _amount = 0m;
            private decimal _balance = 0m;
            public string currency { get; set; }
            public decimal amount
            {
                get
                {
                    decimal.TryParse(_amount.ToString("#.0000"), out _amount);
                    return _amount;
                }
                set
                {
                    _amount = value;
                }
            }

            public decimal balance
            {
                get
                {
                    decimal.TryParse(_balance.ToString("#.0000"), out _balance);
                    return _balance;
                }
                set
                {
                    _balance = value;
                }
            }

        }
        #endregion

        [HttpGet]
        [Route("[controller]/player/check/{account}")]
        [Route("[controller]/{sitename}/player/check/{account}")]
        public CheckPlayerResp CheckPlayer(string account, [FromQuery] GetHeader getHeader, string sitename = "")
        {
            string logName = $"{_tpGameName}_CheckPlayer";
            CheckPlayerResp checkPlayerResp = new()
            {
                status = new GameResponse()
            };

            // check param
            if (getHeader.wtoken == null)
            {
                checkPlayerResp.status.code = "1003";
                checkPlayerResp.status.message = "Error : param invalid";
                checkPlayerResp.status.datetime = GetCurrentGameTime();
                checkPlayerResp.data = false;
                _log2File.Error($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            // check key
            var tokenCurrency = GetTokenCurrency(HttpUtility.UrlDecode(getHeader.wtoken));
            if (!CheckToken(HttpUtility.UrlDecode(getHeader.wtoken), tokenCurrency, sitename))
            {
                checkPlayerResp.status.code = "1100";
                checkPlayerResp.status.message = "Error : wtoken";
                checkPlayerResp.status.datetime = GetCurrentGameTime();
                checkPlayerResp.data = false;
                _log2File.Error($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            // check user (exist status online)
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, account);
            if (tpCheck == null || tpCheck.Locked)
            {
                checkPlayerResp.status.code = "0";
                checkPlayerResp.status.message = "Success";
                checkPlayerResp.status.datetime = GetCurrentGameTime();
                checkPlayerResp.data = false;
                _log2File.Error($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
                return checkPlayerResp;
            }

            checkPlayerResp.status.code = "0";
            checkPlayerResp.status.message = "Success";
            checkPlayerResp.status.datetime = GetCurrentGameTime();
            checkPlayerResp.data = true;

            _log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName);
            return checkPlayerResp;
        }

        [HttpGet]
        [Route("[controller]/transaction/balance/{account}")]
        [Route("[controller]/{sitename}/transaction/balance/{account}")]
        public GetApiResp Balance(string account, string gamecode, [FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getBalanceReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Balance";
            _log2File.Info($"account={account}\r\nwtoken={wtoken}\r\n{gamecode}", logName);

            GetApiResp getBalanceResp = new();
            getBalanceResp.status.datetime = GetCurrentGameTime();
            getBalanceResp.data = null;

            // check param
            if (wtoken == null)
            {
                getBalanceResp.status.code = "1003";
                getBalanceResp.status.message = "Error : param invalid";
                _log2File.Error($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            // check user (exist status online)
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, account);
            if (tpCheck == null)
            {
                // check player account exist
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, account);
                if (tpCustInfo == null)
                {
                    getBalanceResp.status.code = "1006";
                    getBalanceResp.status.message = "Error : account not found";
                    _log2File.Error($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
            }

            // player offline return balance 0 others return cache balance
            var gameType = string.IsNullOrEmpty(gamecode) ? 0 : _thirdPartyBLL.GetGameType((int)_thisTp, gamecode);
            var tpCustBalance = (_environment.IsDevelopment()) ? _tpCustInfoBLL.GetBalance((int)_thisTp, account, (int)gameType) : _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, account, (int)gameType);
            getBalanceResp.data = new BalanceData();
            getBalanceResp.status.code = "0";
            getBalanceResp.status.message = "Success";
            getBalanceResp.status.datetime = GetCurrentGameTime();
            getBalanceResp.data.balance = (tpCheck == null) ? 0 : _tools.Floor(tpCustBalance.Balance, 4);
            getBalanceResp.data.currency = tpCustBalance.TpCurrencyId;

            _log2File.Info($"account={account}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
            return getBalanceResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/bet")]
        [Route("[controller]/{sitename}/transaction/game/bet")]
        public GetApiResp Bet([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getBetReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Bet";
            var json = JsonConvert.SerializeObject(getBetReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var betReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            /*** TEST ***/
            //if (environment.IsDevelopment() && getBetReq.ContainsKey("gamecode"))
            //{
            //    var gameTypeDev = thirdPartyBLL.GetGameType((int)thisTp, betReq.gamecode);
            //    if (gameTypeDev == GameType.CQ9_Fish)
            //    {
            //        return Rollout(wtoken, getBetReq);
            //    }
            //}
            /*** TEST ***/

            GetApiResp getBetResp = new();
            getBetResp.status.code = "1100";
            getBetResp.status.message = "Error";
            getBetResp.status.datetime = GetCurrentGameTime();
            getBetResp.data = null;

            if (betReq.amount <= 0)
            {
                // 當amount不得為負值為負值時則反回錯誤編碼1003
                getBetResp.status.code = "1003";
                getBetResp.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check param
            if (wtoken == null || betReq.account == null || betReq.eventTime == null || betReq.gamecode == null || betReq.gamehall == null || betReq.roundid == null || betReq.mtcode == null)
            {
                getBetResp.status.code = "1003";
                getBetResp.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            long chkEventTimeParse = CheckTimeParse(betReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                getBetResp.status.code = "1004";
                getBetResp.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }
            betReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                getBetResp.status.code = "1100";
                getBetResp.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check session
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, betReq.account);
            if (tpCheck == null || tpCheck.Locked || tpCheck.Token != HttpUtility.UrlDecode(betReq.session))
            {
                getBetResp.status.code = "1006";
                getBetResp.status.message = "Error : account not found";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, betReq.gamecode);
            if (gameType != GameType.CQ9_RNG)
            {
                getBetResp.status.code = "1100";
                getBetResp.status.message = "Error : gamecode is not slot";
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            try
            {
                int result = _bettingBLL.PlaceBetRNG_CQ9(betReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getBetResp.data = new BalanceData();
                    getBetResp.data.balance = _tools.Floor(tpAffterAmount, 4);
                    getBetResp.data.currency = tokenCurrency;
                    getBetResp.status.code = "0";
                    getBetResp.status.message = "Success";
                    getBetResp.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                    return getBetResp;
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    getBetResp.status.code = "2009";
                    getBetResp.status.message = "Error : mtcode exist";
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    getBetResp.status.code = "1005";
                    getBetResp.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex.Message}", logName, ex);
                return getBetResp;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(betReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
            return getBetResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/endround")]
        [Route("[controller]/{sitename}/transaction/game/endround")]
        public GetApiResp EndRound([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getEndRoundReq, string sitename = "")
        {
            var result = 0;
            string logName = $"{_tpGameName}_EndRound";
            var json = JsonConvert.SerializeObject(getEndRoundReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var endRoundReq = JsonConvert.DeserializeObject<EndRoundDataRNG_CQ9>(json);

            /*** TEST ***/
            //if (environment.IsDevelopment() && getEndRoundReq.ContainsKey("gametype"))
            //{
            //    var reqGameType = getEndRoundReq["gametype"];
            //    if (reqGameType == "fish")
            //    {
            //        return Rollin(wtoken, getEndRoundReq);
            //    }
            //}
            /*** TEST ***/

            GetApiResp getEndRoundResp = new GetApiResp();
            getEndRoundResp.status.code = "1100";
            getEndRoundResp.status.message = "Error";
            getEndRoundResp.status.datetime = GetCurrentGameTime();
            getEndRoundResp.data = null;

            // check param
            if (wtoken == null || endRoundReq.account == null || endRoundReq.gamecode == null || endRoundReq.gamehall == null || endRoundReq.roundid == null || endRoundReq.data == null || endRoundReq.createTime == null)
            {
                getEndRoundResp.status.code = "1003";
                getEndRoundResp.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(endRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                return getEndRoundResp;
            }
            try
            {
                // check key
                var tokenCurrency = GetTokenCurrency(wtoken);
                if (!CheckToken(wtoken, tokenCurrency, sitename))
                {
                    getEndRoundResp.status.code = "1100";
                    getEndRoundResp.status.message = "Error : wtoken";
                    _log2File.Error($"{JsonConvert.SerializeObject(endRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }

                // check gametype
                var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, endRoundReq.gamecode);
                if (gameType != GameType.CQ9_RNG)
                {
                    getEndRoundResp.status.code = "1100";
                    getEndRoundResp.status.message = "Error : gamecode is not slot";
                    _log2File.Error($"{JsonConvert.SerializeObject(endRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }

                var mtcode = string.Empty;
                // check item param
                foreach (var item in endRoundReq.detail)
                {
                    if (item.amount < 0)
                    {
                        // amount不得為負值為負值時則反回錯誤編碼1003
                        getEndRoundResp.status.code = "1003";
                        getEndRoundResp.status.message = "Error : insufficient amount";
                        _log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                        return getEndRoundResp;
                    }

                    long chkEventTimeParse = CheckTimeParse(item.eventtime);
                    if (chkEventTimeParse == 0)
                    {
                        // 當eventTime格式錯誤時回傳 1004
                        getEndRoundResp.status.code = "1004";
                        getEndRoundResp.status.message = "Error : invalid eventTime";
                        _log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                        return getEndRoundResp;
                    }
                    item.eventUnix = chkEventTimeParse;
                    // 紀錄最後的mtcode
                    mtcode = item.mtcode;
                }


                long chkCreateTimeParse = CheckTimeParse(endRoundReq.createTime);
                endRoundReq.createUnix = chkCreateTimeParse;

                result = _settledBLL.PayoutRNG_CQ9(endRoundReq, (int)gameType, out decimal tpAffterAmount);

                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, endRoundReq.account, (int)gameType);
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Payout && i.mtcode == mtcode);
                        if (record == null)
                        {
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAffterAmount = record.Balance;
                        }
                    }

                    getEndRoundResp.data = new BalanceData();
                    getEndRoundResp.data.balance = _tools.Floor(tpAffterAmount, 4);
                    getEndRoundResp.data.currency = tokenCurrency;
                    getEndRoundResp.status.code = "0";
                    getEndRoundResp.status.message = "Success";
                    getEndRoundResp.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
                    return getEndRoundResp;
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    getEndRoundResp.status.code = "1006";
                    getEndRoundResp.status.message = "Error : account not found";
                }
                else
                {
                    // 1100 對方重送資料
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}\r\n{ex.Message}", logName, ex);
                return getEndRoundResp;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(getEndRoundReq)}\r\n{JsonConvert.SerializeObject(getEndRoundResp)}", logName);
            return getEndRoundResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/refund")]
        [Route("[controller]/{sitename}/transaction/game/refund")]
        public GetApiResp Refund([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getRefundReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Refund";
            var json = JsonConvert.SerializeObject(getRefundReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var refundReq = JsonConvert.DeserializeObject<RefundDataRNG_CQ9>(json);

            GetApiResp getRefundResp = new GetApiResp();
            getRefundResp.status.code = "1100";
            getRefundResp.status.message = "Error";
            getRefundResp.status.datetime = GetCurrentGameTime();
            getRefundResp.data = null;

            // check param
            if (wtoken == null || refundReq.mtcode == null)
            {
                getRefundResp.status.code = "1003";
                getRefundResp.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                return getRefundResp;
            }

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                getRefundResp.status.code = "1100";
                getRefundResp.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                return getRefundResp;
            }

            try
            {
                var record = _settledBLL.GetRecordRNG_CQ9(refundReq.mtcode);
                if (record == null || record.Count() == 0)
                {
                    // 當refund未查詢到紀錄時回傳該錯誤編碼1014
                    getRefundResp.status.code = "1014";
                    getRefundResp.status.message = "Error : record not found";
                    _log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                    return getRefundResp;
                }
                else if (record.FirstOrDefault()?.TicketStatus == (int)TicketStatus.Refund)
                {
                    var refundData = record.First();
                    var refGameType = refundData.TableType;
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, refundReq.account, (int)refGameType);
                    // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                    var balance = refundData.Balance;
                    if (refGameType != (int)GameType.CQ9_RNG)
                    {
                        balance += Math.Abs(refundData.amount);
                    }

                    getRefundResp.status.code = "0";
                    getRefundResp.status.message = "Success";
                    getRefundResp.status.datetime = GetCurrentGameTime();
                    getRefundResp.data = new BalanceData();
                    getRefundResp.data.balance = _tools.Floor(balance, 4);
                    getRefundResp.data.currency = tpCustBalance.TpCurrencyId;
                    _log2File.Info($"refunded\r\n{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                    return getRefundResp;
                }

                refundReq.amount = record.First().amount;
                refundReq.roundid = record.First().roundid;
                var gameType = record.First().TableType;

                var result = _settledBLL.RefundRNG_CQ9(refundReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, refundReq.account, (int)gameType);
                    getRefundResp.status.code = "0";
                    getRefundResp.status.message = "Success";
                    getRefundResp.status.datetime = GetCurrentGameTime();
                    getRefundResp.data = new BalanceData();
                    getRefundResp.data.balance = _tools.Floor(tpCustBalance.Balance, 4);
                    getRefundResp.data.currency = tpCustBalance.TpCurrencyId;
                    _log2File.Info($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
                    return getRefundResp;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}\r\n{ex.Message}", logName, ex);
                return getRefundResp;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(refundReq)}\r\n{JsonConvert.SerializeObject(getRefundResp)}", logName);
            return getRefundResp;
        }

        [HttpGet]
        [Route("[controller]/transaction/record/{mtcode}")]
        [Route("[controller]/{sitename}/transaction/record/{mtcode}")]
        public GetRecordResp Record(string mtcode, [FromHeader(Name = "wtoken")] string wtoken, string sitename = "")
        {
            string logName = $"{_tpGameName}_Record";
            _log2File.Info($"mtcode={mtcode}\r\nwtoken={wtoken}", logName);

            GetRecordResp getRecordResp = new GetRecordResp();
            getRecordResp.status.code = "1100";
            getRecordResp.status.message = "Error";
            getRecordResp.status.datetime = GetCurrentGameTime();
            getRecordResp.data = null;

            if (wtoken == null)
            {
                getRecordResp.status.code = "1003";
                getRecordResp.status.message = "Error : param invalid";
                _log2File.Error($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                getRecordResp.status.code = "1100";
                getRecordResp.status.message = "Error : wtoken";
                _log2File.Error($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }
            string[] sType = mtcode.Split("-");
            if (sType.Count() == 1)
            {
                getRecordResp.status.code = "1014";
                getRecordResp.status.message = "Error : mtcode format invalid";
                _log2File.Error($"mtcode={mtcode}\r\nwtoken={wtoken}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
                return getRecordResp;
            }

            var data = _settledBLL.GetRecordRNG_CQ9(mtcode);
            if (data.Count() == 0)
            {
                // 當未查詢到紀錄時回傳該錯誤編碼1014
                getRecordResp.status.code = "1014";
                getRecordResp.status.message = "Error : record not found";
                getRecordResp.status.datetime = GetCurrentGameTime();
            }
            else
            {
                getRecordResp.data = new RecordData();
                getRecordResp.data._id = mtcode;
                getRecordResp.data.action = data.ElementAt(0).Actions;
                getRecordResp.data.target.account = data.ElementAt(0).account;
                getRecordResp.data.status.createtime = data.ElementAt(0).CreateFormat;
                getRecordResp.data.status.endtime = data.ElementAt(0).UpdateFormat;
                getRecordResp.data.status.status = (data.ElementAt(0).TicketStatus == (int)TicketStatus.Refund) ? "refund" : "success";
                getRecordResp.data.status.message = "success";
                getRecordResp.data.currency = data.ElementAt(0).currency;
                getRecordResp.data.before = data.First().Before;
                getRecordResp.data.balance = data.Last().Balance;
                getRecordResp.data.Event = new List<RecordEvent>();

                foreach (var item in data)
                {
                    RecordEvent refRecordEvent = new RecordEvent();
                    refRecordEvent.mtcode = item.mtcode;
                    refRecordEvent.amount = Math.Abs(item.amount);
                    refRecordEvent.eventtime = item.eventFormat;
                    getRecordResp.data.Event.Add(refRecordEvent);
                }

                getRecordResp.status.code = "0";
                getRecordResp.status.message = "Success";
                getRecordResp.status.datetime = GetCurrentGameTime();
            }

            _log2File.Info($"mtcode={mtcode}\r\n{JsonConvert.SerializeObject(getRecordResp)}", logName);
            return getRecordResp;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/debit")]
        [Route("[controller]/{sitename}/transaction/game/debit")]
        public GetApiResp Debit([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getDebitReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Debit";
            var json = JsonConvert.SerializeObject(getDebitReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var debitReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // 當為負值時則反回錯誤編碼1003
            if (debitReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || debitReq.account == null || debitReq.eventTime == null || debitReq.gamecode == null || debitReq.gamehall == null || debitReq.roundid == null || debitReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(debitReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            debitReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, debitReq.gamecode);
            if (gameType == GameType.CQ9_WCC)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : gamecode is WCC";
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                int result = _settledBLL.DebitRNG_CQ9(debitReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, debitReq.account, (int)gameType);
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(debitReq.mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Payout && i.mtcode == debitReq.mtcode);
                        if (record == null)
                        {
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAffterAmount = record.Balance;
                        }
                    }

                    apiResponse.data = new BalanceData();
                    apiResponse.data.balance = _tools.Floor(tpAffterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    apiResponse.status.code = "1005";
                    apiResponse.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(debitReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/credit")]
        [Route("[controller]/{sitename}/transaction/game/credit")]
        public GetApiResp Credit([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getCreditReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Credit";
            var json = JsonConvert.SerializeObject(getCreditReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var creditReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // 當為負值時則反回錯誤編碼1003
            if (creditReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || creditReq.account == null || creditReq.eventTime == null || creditReq.gamecode == null || creditReq.gamehall == null || creditReq.roundid == null || creditReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(creditReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            creditReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(getCreditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, creditReq.gamecode);
            if (gameType == GameType.CQ9_WCC)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : gamecode is WCC";
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                var result = _settledBLL.CreditRNG_CQ9(creditReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, creditReq.account, (int)gameType);
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(creditReq.mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Payout && i.mtcode == creditReq.mtcode);
                        if (record == null)
                        {
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAffterAmount = record.Balance;
                        }
                    }

                    apiResponse.data = new BalanceData();
                    apiResponse.data.balance = _tools.Floor(tpAffterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(creditReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/user/payoff")]
        [Route("[controller]/{sitename}/transaction/user/payoff")]
        public GetApiResp Payoff([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getPayoffReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Payoff";
            var json = JsonConvert.SerializeObject(getPayoffReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var payoffReq = JsonConvert.DeserializeObject<PayoffDataRNG_CQ9>(json);

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // 當為負值時則反回錯誤編碼1003
            if (payoffReq.amount <= 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || payoffReq.account == null || payoffReq.eventTime == null || payoffReq.mtcode == null)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(payoffReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            payoffReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                var result = _settledBLL.RewardRNG_CQ9(payoffReq, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, payoffReq.account);/*Just for TpCurrencyId*/
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(payoffReq.mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Bonus && i.mtcode == payoffReq.mtcode);
                        if (record == null)
                        {
                            tpAffterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAffterAmount = record.Balance;
                        }
                    }
                    apiResponse.data = new BalanceData();
                    apiResponse.data.balance = _tools.Floor(tpAffterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(payoffReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/rollout")]
        [Route("[controller]/{sitename}/transaction/game/rollout")]
        public GetApiResp Rollout([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getRolloutReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Rollout";
            var validGameTypes = new[] { GameType.CQ9_Fish, GameType.CQ9_WCC };
            var json = JsonConvert.SerializeObject(getRolloutReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var rolloutReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);
            var result = 0;
            var tpAfterAmount = 0.00m;

            /*** TEST ***/
            //if (environment.IsDevelopment() && getRolloutReq.ContainsKey("gamecode"))
            //{
            //    var gameTypeDev = thirdPartyBLL.GetGameType((int)thisTp, rolloutReq.gamecode);
            //    if (gameTypeDev == GameType.CQ9_RNG)
            //    {
            //        return Bet(wtoken, getRolloutReq);
            //    }
            //}
            /*** TEST ***/

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // 當為負值時則反回錯誤編碼1003
            if (rolloutReq.amount < 0 || !getRolloutReq.ContainsKey("amount"))
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || !getRolloutReq.ContainsKey("account") || !getRolloutReq.ContainsKey("eventTime") || !getRolloutReq.ContainsKey("gamecode") || !getRolloutReq.ContainsKey("gamehall") || !getRolloutReq.ContainsKey("roundid") || !getRolloutReq.ContainsKey("mtcode"))
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(rolloutReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            rolloutReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check session
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, rolloutReq.account);
            if (tpCheck == null || tpCheck.Locked || tpCheck.Token != HttpUtility.UrlDecode(rolloutReq.session))
            {
                apiResponse.status.code = "1006";
                apiResponse.status.message = "Error : account not found";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, rolloutReq.gamecode);
            if(rolloutReq.gamecode == "GB11")
            {
                gameType = GameType.CQ9_Fish;
            }
            if (!validGameTypes.Contains(gameType))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : gamecode is not fish or WCC";
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                switch (gameType)
                {
                    case GameType.CQ9_Fish:
                        result = _bettingBLL.RolloutFish_CQ9(rolloutReq, (int)gameType, out decimal tpAfterAmountFish);
                        tpAfterAmount = tpAfterAmountFish;
                        break;
                    case GameType.CQ9_WCC:
                        result = _bettingBLL.RolloutWCC_CQ9(rolloutReq, (int)gameType, out decimal tpAfterAmountWWC);
                        tpAfterAmount = tpAfterAmountWWC;
                        break;
                }
                //result = _bettingBLL.RolloutFish_CQ9(rolloutReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, rolloutReq.account, (int)gameType);
                    if (tpAfterAmount == 0)
                    {
                        tpAfterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(rolloutReq.mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Bet && i.mtcode == rolloutReq.mtcode);
                        if (record == null)
                        {
                            tpAfterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAfterAmount = record.Balance;
                        }
                    }

                    apiResponse.data = new BalanceData();
                    apiResponse.data.balance = _tools.Floor(tpAfterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    apiResponse.status.code = "2009";
                    apiResponse.status.message = "Error : mtcode exist";
                }
                else if (tpAfterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    apiResponse.status.code = "1005";
                    apiResponse.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(rolloutReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/takeall")]
        [Route("[controller]/{sitename}/transaction/game/takeall")]
        public GetTakeAllResp TakeAll([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getTakeAllReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_TakeAll";
            var json = JsonConvert.SerializeObject(getTakeAllReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var takeAllReq = JsonConvert.DeserializeObject<BettingDataRNG_CQ9>(json);

            GetTakeAllResp apiResponse = new GetTakeAllResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // check param
            if (wtoken == null || !getTakeAllReq.ContainsKey("account") || !getTakeAllReq.ContainsKey("eventTime") || !getTakeAllReq.ContainsKey("gamecode") || !getTakeAllReq.ContainsKey("gamehall") || !getTakeAllReq.ContainsKey("roundid") || !getTakeAllReq.ContainsKey("mtcode"))
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(takeAllReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            takeAllReq.eventUnix = chkEventTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check session
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, takeAllReq.account);
            if (tpCheck == null || tpCheck.Locked || tpCheck.Token != HttpUtility.UrlDecode(takeAllReq.session))
            {
                apiResponse.status.code = "1006";
                apiResponse.status.message = "Error : account not found";
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, takeAllReq.gamecode);
            if (rolloutReq.gamecode == "GB11")
            {
                gameType = GameType.CQ9_Fish;
            }
            if (gameType != GameType.CQ9_Fish)
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : gamecode is not fish";
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, takeAllReq.account, (int)gameType);
                takeAllReq.amount = tpCustBalance.Balance;
                if (takeAllReq.amount < 0)
                {
                    var record = _settledBLL.GetRecordRNG_CQ9(takeAllReq.mtcode);
                    if (record != null && record.Count() > 0)
                    {
                        // 當mtcode重複時則反回錯誤編碼 2009
                        apiResponse.status.code = "2009";
                        apiResponse.status.message = "Error : mtcode exist";
                    }
                    else
                    {
                        // 當餘額不足時回傳 1005
                        apiResponse.status.code = "1005";
                        apiResponse.status.message = "Error : insufficient amount";
                    }
                    _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }

                int result = _bettingBLL.TakeAllFish_CQ9(takeAllReq, (int)gameType, out decimal tpAffterAmount);
                if (result > 0)
                {
                    apiResponse.data = new TakeAllData();
                    apiResponse.data.amount = _tools.Floor(tpCustBalance.Balance, 4);
                    apiResponse.data.balance = _tools.Floor(tpAffterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -1)
                {
                    // 當mtcode重複時則反回錯誤編碼 2009
                    apiResponse.status.code = "2009";
                    apiResponse.status.message = "Error : mtcode exist";
                }
                else if (tpAffterAmount < 0)
                {
                    // 當餘額不足時回傳 1005
                    apiResponse.status.code = "1005";
                    apiResponse.status.message = "Error : insufficient amount";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(takeAllReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [Route("[controller]/transaction/game/rollin")]
        [Route("[controller]/{sitename}/transaction/game/rollin")]
        public GetApiResp Rollin([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getRollinReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_Rollin";
            var validGameTypes = new[] { GameType.CQ9_Fish, GameType.CQ9_WCC };
            var json = JsonConvert.SerializeObject(getRollinReq);
            _log2File.Info($"wtoken={wtoken}\r\n{json}", logName);
            var rollinReq = JsonConvert.DeserializeObject<RollinDataFish_CQ9>(json);
            var result = 0;
            var tpAfterAmount = 0.00m;

            /*** TEST ***/
            //if (environment.IsDevelopment() && getRollinReq.ContainsKey("gamecode") && !getRollinReq.ContainsKey("gametype"))
            //{
            //    var gameTypeDev = thirdPartyBLL.GetGameType((int)thisTp, rollinReq.gamecode);
            //    if (gameTypeDev == GameType.CQ9_RNG)
            //    {
            //        return EndRound(wtoken, getRollinReq);
            //    }
            //}
            /*** TEST ***/

            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1100";
            apiResponse.status.message = "Error";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;

            // 當為負值時則反回錯誤編碼1003
            if (rollinReq.amount < 0)
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : insufficient amount";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check param
            if (wtoken == null || !getRollinReq.ContainsKey("account") || !getRollinReq.ContainsKey("eventTime") || !getRollinReq.ContainsKey("gamecode") || !getRollinReq.ContainsKey("gamehall") || !getRollinReq.ContainsKey("roundid") || !getRollinReq.ContainsKey("mtcode") || !getRollinReq.ContainsKey("createTime") || !getRollinReq.ContainsKey("bet") || !getRollinReq.ContainsKey("win") || !getRollinReq.ContainsKey("gametype") || !getRollinReq.ContainsKey("rake") || !getRollinReq.ContainsKey("amount"))
            {
                apiResponse.status.code = "1003";
                apiResponse.status.message = "Error : param invalid";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            long chkEventTimeParse = CheckTimeParse(rollinReq.eventTime);
            if (chkEventTimeParse == 0)
            {
                // 當eventTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid eventTime";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            rollinReq.eventUnix = chkEventTimeParse;

            long chkCreateTimeParse = CheckTimeParse(rollinReq.createTime);
            if (chkCreateTimeParse == 0)
            {
                // 當createTime格式錯誤時回傳 1004
                apiResponse.status.code = "1004";
                apiResponse.status.message = "Error : invalid createTime";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }
            rollinReq.createUnix = chkCreateTimeParse;

            // check key
            var tokenCurrency = GetTokenCurrency(wtoken);
            if (!CheckToken(wtoken, tokenCurrency, sitename))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : wtoken";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            // check gametype
            var gameType = _thirdPartyBLL.GetGameType((int)_thisTp, rollinReq.gamecode);
            if (rolloutReq.gamecode == "GB11")
            {
                gameType = GameType.CQ9_Fish;
            }
            if (!validGameTypes.Contains(gameType))
            {
                apiResponse.status.code = "1100";
                apiResponse.status.message = "Error : gamecode is not fish or WCC";
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                return apiResponse;
            }

            try
            {
                switch (gameType)
                {
                    case GameType.CQ9_Fish:
                        result = _settledBLL.RollinFish_CQ9(rollinReq, gameType, out decimal tpAfterAmountFish);
                        tpAfterAmount = tpAfterAmountFish;
                        break;
                    case GameType.CQ9_WCC:
                        result = _settledBLL.RollinWCC_CQ9(rollinReq, gameType, out decimal tpAfterAmountWCC);
                        tpAfterAmount = tpAfterAmountWCC;
                        break;
                }

                if (result > 0 || result == -1)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, rollinReq.account, (int)gameType);
                    if (tpAfterAmount == 0)
                    {
                        tpAfterAmount = tpCustBalance.Balance;
                    }
                    if (result == -1)
                    {
                        var record = _settledBLL.GetRecordRNG_CQ9(rollinReq.mtcode).FirstOrDefault(i => i.TicketType == (int)BettransType.Payout && i.mtcode == rollinReq.mtcode);
                        if (record == null)
                        {
                            tpAfterAmount = tpCustBalance.Balance;
                        }
                        else
                        {
                            // 請求若與前一次mtcode相同時，回應上一次mtcode成功的餘額，不必再對該玩家進行額度的增減
                            tpAfterAmount = record.Balance;
                        }
                    }
                    apiResponse.data = new BalanceData();
                    apiResponse.data.balance = _tools.Floor(tpAfterAmount, 4);
                    apiResponse.data.currency = tpCustBalance.TpCurrencyId;
                    apiResponse.status.code = "0";
                    apiResponse.status.message = "Success";
                    apiResponse.status.datetime = GetCurrentGameTime();
                    _log2File.Info($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
                    return apiResponse;
                }
                else if (result == -21)
                {
                    // 查無玩家時回傳該編碼
                    apiResponse.status.code = "1006";
                    apiResponse.status.message = "Error : account not found";
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}\r\n{ex.Message}", logName, ex);
                return apiResponse;
            }

            _log2File.Error($"{JsonConvert.SerializeObject(rollinReq)}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        [HttpPost]
        [HttpGet]
        [Route("[controller]/{path1?}/{path2?}/{path3?}/{path4?}")]
        [Route("[controller]/{sitename}/{path1?}/{path2?}/{path3?}/{path4?}")]
        public GetApiResp NotFound([FromHeader(Name = "wtoken")] string wtoken, [FromForm] Dictionary<string, string> getReq, string sitename = "")
        {
            string logName = $"{_tpGameName}_NotFound";
            var json = JsonConvert.SerializeObject(getReq);
            GetApiResp apiResponse = new GetApiResp();
            apiResponse.status.code = "1002";
            apiResponse.status.message = "Error: 404";
            apiResponse.status.datetime = GetCurrentGameTime();
            apiResponse.data = null;
            _log2File.Error($"wtoken={wtoken}\r\n{json}\r\n{JsonConvert.SerializeObject(apiResponse)}", logName);
            return apiResponse;
        }

        private static long CheckTimeParse(string time)
        {
            DateTime dtFormat;
            if (!DateTime.TryParse(time, out dtFormat))
            {
                return 0;
            }
            else
            {
                string[] chkDt = time.Split("-");
                int cntDt = chkDt.Count();
                if (cntDt != 4 || chkDt[3] != "04:00")
                {
                    return 0;
                }
            }
            return new DateTimeOffset(dtFormat).ToUnixTimeSeconds();
        }

        private static string GetCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffK");
        }

        private static string GetTokenCurrency(string token)
        {
            var parts = token.Split('.');
            var header = parts[0];
            var payload = parts[1];
            byte[] crypto = Base64UrlDecode(parts[2]);
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(payload));
            var payloadData = JObject.Parse(payloadJson);
            var tokenData = JsonConvert.DeserializeObject<Payload>(payloadJson);
            if (tokenData.account.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            {
                tokenData.currency = "TEST";
            }
            return tokenData.currency;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: output += "=="; break; // Two pad chars
                case 3: output += "="; break; // One pad char
                default: throw new System.Exception("Illegal base64url string!");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
        }

        private bool CheckToken(string wtoken, string tokenCurrency, string sitename)
        {
            var config = GetSiteConfig(sitename);
            var token = config[$"API_Token_{tokenCurrency}"];
            if (token == wtoken)
            {
                return true;
            }
            return false;
        }

        private Dictionary<string, string> GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return _defaultConfig;
            }

            return _siteNameConfigs.TryGetValue(siteName, out var result) ? result : _defaultConfig;
        }
    }
}