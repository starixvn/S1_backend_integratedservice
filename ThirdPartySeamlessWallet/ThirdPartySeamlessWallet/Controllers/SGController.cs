using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SGController : ControllerBase
    {
        private readonly Log2File _log2File;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly AuthBLL _authBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tp _thisTp;
        private readonly string _tpName = Tp.SG.ToString();
        private readonly Dictionary<int, SGConfig> _siteConfigDic;

        public SGController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _thisTp = Tp.SG;
            _log2File = provider.GetRequiredService<Log2File>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SGConfig>(_tpName);
        }

        #region Request

        public class AuthorizeReq
        {
            public string acctId { get; set; }
            public string language { get; set; }
            public string merchantCode { get; set; }
            public string token { get; set; }
            public string serialNo { get; set; }
            public string gameCode { get; set; }
        }

        public class GetBalanceReq
        {
            public string acctId { get; set; }
            public string merchantCode { get; set; }
            public string serialNo { get; set; }
            public string gameCode { get; set; }
            public string token { get; set; }
        }

        public class TransferReq : BettingDataRNG_SG
        {

        }

        #endregion

        #region Response

        public class Resp
        {
            public string merchantCode { get; set; }
            public string msg { get; set; }
            public int code { get; set; }
            public string serialNo { get; set; }
        }

        public class AcctInfo
        {
            public string acctId { get; set; }
            public decimal balance { get; set; }
            public string userName { get; set; }
            public string currency { get; set; }
        }

        public class AuthorizeResp : Resp
        {
            public AcctInfo acctInfo { get; set; }
        }

        public class GetBalanceResp : Resp
        {
            public AcctInfo acctInfo { get; set; }
        }

        public class TransferResp : Resp
        {
            public string transferId { get; set; }
            public string merchantTxId { get; set; }
            public string acctId { get; set; }
            public decimal balance { get; set; }
        }
        #endregion

        [Route("")]
        [HttpPost]
        public async Task<Resp> IndexAsync()
        {
            Resp resp = new();
            Request.Headers.TryGetValue("API", out StringValues vs);
            var api = vs.FirstOrDefault();
            var logName = $"{_tpName}_{api}";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            if (api.Equals("authorize", StringComparison.OrdinalIgnoreCase))
            {
                AuthorizeResp authorizeResp = new()
                {
                    code = 1,
                    msg = "System Error"
                };
                var authorizeReq = JsonConvert.DeserializeObject<AuthorizeReq>(Content);

                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(authorizeReq.token));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    string authToken = sessionKeyArr[0]; /* 1 Time*/
                    string sessionToken = sessionKeyArr[1];/* Long Time*/
                    if (_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
                        if (tpCheck == null || tpCheck.Token != sessionToken)
                        {
                            authorizeResp.code = 50104;
                            authorizeResp.msg = "Token Validation Failed.";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", logName);
                            return authorizeResp;
                        }
                        else if (tpCheck.Locked)
                        {
                            authorizeResp.code = 50102;
                            authorizeResp.msg = "Acct Locked";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", logName);
                            return authorizeResp;
                        }
                        else
                        {
                            var config = GetConfig(tpCustInfoAuth.SiteId);
                            if (string.IsNullOrEmpty(config.merchantCode))
                            {
                                authorizeResp.code = 10113;
                                authorizeResp.msg = "Merchant Not Found";
                                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", logName);
                                return authorizeResp;
                            }

                            var gameType = _thirdPartyBLL.GetGameType_SG(authorizeReq.gameCode);
                            _tpCustInfoBLL.SetCustGotoGame(tpCustInfoAuth.CustId, (int)_thisTp, (int)gameType);
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
                            authorizeResp.acctInfo = new AcctInfo
                            {
                                acctId = tpCustInfoAuth.TpUserName,
                                currency = tpCustInfoAuth.TpCurrencyId,
                                userName = tpCustInfoAuth.TpUserName,
                                balance = tpCustBalance.Balance
                            };

                            authorizeResp.merchantCode = config.merchantCode;
                            authorizeResp.serialNo = authorizeReq.serialNo;
                            authorizeResp.code = 0;
                            authorizeResp.msg = "Success";
                        }
                    }
                    else
                    {
                        authorizeResp.code = 50104;
                        authorizeResp.msg = "Token Validation Failed.";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", logName);
                        return authorizeResp;
                    }
                }
                catch (Exception ex)
                {
                    authorizeResp.code = 1;
                    authorizeResp.msg = "System Error";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}\r\n{ex}", logName, ex);
                }

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", logName);
                return authorizeResp;
            }
            else if (api.Equals("getBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceResp getBalanceResp = new()
                {
                    code = 1,
                    msg = "System Error"
                };
                var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(Content);
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getBalanceReq.token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getBalanceReq.acctId);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                {
                    getBalanceResp.code = 50104;
                    getBalanceResp.msg = "Token Validation Failed";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
                else if (tpCheck.Locked)
                {
                    getBalanceResp.code = 50102;
                    getBalanceResp.msg = "Acct Locked";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                    return getBalanceResp;
                }
                else
                {
                    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getBalanceReq.acctId);
                    var config = GetConfig(tpCustInfo.SiteId);
                    if (string.IsNullOrEmpty(config.merchantCode))
                    {
                        getBalanceResp.code = 10113;
                        getBalanceResp.msg = "Merchant Not Found";
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                        return getBalanceResp;
                    }

                    var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)_thisTp);
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, getBalanceReq.acctId, gameType);
                    getBalanceResp.acctInfo = new AcctInfo
                    {
                        acctId = getBalanceReq.acctId,
                        currency = tpCustBalance.TpCurrencyId,
                        userName = getBalanceReq.acctId,
                        balance = tpCustBalance.Balance
                    };
                    getBalanceResp.serialNo = getBalanceReq.serialNo;
                    getBalanceResp.merchantCode = config.merchantCode;

                    getBalanceResp.code = 0;
                    getBalanceResp.msg = "Success";
                }

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }
            else if (api.Equals("transfer", StringComparison.OrdinalIgnoreCase))
            {
                TransferResp transferResp = new()
                {
                    code = 1,
                    msg = "System Error"
                };

                var transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);

                logName = $"{_tpName}_{api}_{transferReq.type}";

                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, transferReq.acctId);
                var config = GetConfig(tpCustInfo.SiteId);
                if (string.IsNullOrEmpty(config.merchantCode))
                {
                    transferResp.code = 10113;
                    transferResp.msg = "Merchant Not Found";
                    _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                    return transferResp;
                }

                transferResp.acctId = transferReq.acctId;
                transferResp.transferId = transferReq.transferId;
                transferResp.merchantTxId = transferReq.transferId;
                transferResp.serialNo = transferReq.serialNo;
                transferResp.merchantCode = config.merchantCode;

                var gameType = _thirdPartyBLL.GetGameType_SG(transferReq.gameCode);

                if (transferReq.type == TransferType.PlaceBet)
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(transferReq.token));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    string authToken = sessionKeyArr[0]; /* 1 Time*/
                    string sessionToken = sessionKeyArr[1];/* Long Time*/
                    try
                    {
                        var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, transferReq.acctId);
                        if (tpCheck == null)
                        {
                            transferResp.code = 50104;
                            transferResp.msg = "Token Validation Failed";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            transferResp.code = 50104;
                            transferResp.msg = "Token Validation Failed";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                        else if (tpCheck.Locked)
                        {
                            transferResp.code = 50102;
                            transferResp.msg = "Acct Locked";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }

                        if (_bettingBLL.PlaceBetRNG_SG(transferReq, (int)gameType, out decimal tpAffterAmount) > 0)
                        {
                            if (transferReq.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                                transferResp.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                transferResp.balance = tpAffterAmount;
                            }
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 50110;
                            transferResp.msg = "Insufficient Balance";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                        else/*Can't Insert Bet*/
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 1;
                            transferResp.msg = "System Error";
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", logName, ex);
                        throw;
                    }
                }
                else if (transferReq.type == TransferType.CancelBet)
                {
                    try
                    {
                        if (_settledBLL.CancelBetRNG_SG(transferReq, (int)gameType, out decimal tpAffterAmount) > 0)
                        {
                            if (transferReq.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                                transferResp.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                transferResp.balance = tpAffterAmount;
                            }
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                        else
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                    }
                    catch (Exception ex)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex}", logName, ex);
                        throw;
                    }
                }
                else if (transferReq.type == TransferType.Payout)
                {
                    try
                    {
                        int result = _settledBLL.PayoutRNG_SG(transferReq, (int)gameType, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.amount > 0)
                            {
                                transferResp.balance = tpAffterAmount;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                            else if (transferReq.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                                transferResp.balance = tpCustBalance.Balance;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                        else
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", logName, ex);
                        throw;
                    }
                }
                else if (transferReq.type == TransferType.Bonus)
                {
                    try
                    {
                        int result = _settledBLL.PayoutRNG_SG(transferReq, (int)gameType, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.amount > 0)
                            {
                                transferResp.balance = tpAffterAmount;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                            else if (transferReq.amount == 0)
                            {
                                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                                transferResp.balance = tpCustBalance.Balance;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, transferReq.acctId, (int)gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                            _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                        else
                        {
                            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                            return transferResp;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", logName, ex);
                        throw;
                    }
                }

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", logName);
                return transferResp;
            }

            return resp;
        }

        private SGConfig GetConfig(int siteId)
        {
            return _siteConfigDic[siteId];
        }
    }
}