using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AllbetController : ControllerBase
    {
        private readonly Tools tools;
        private readonly Log2File log2File;
        private readonly ILogger<AllbetController> _logger;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;
        private readonly int gameType = 0;
        private static IAllbetBll _AllbetBll { get; set; }
        

        public AllbetController(IServiceProvider provider)
        {
            tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<AllbetController>>();
            log2File = provider.GetRequiredService<Log2File>();
            thisTp = Tp.Allbet;
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            gameType = (int)GameType.Allbet_LiveCasino;
            _AllbetBll = new AllbetBll(provider);
        }
        public class Resp
        {
            public string error_code { get; set; }
            public string message { get; set; }
            public decimal balance { get; set; }
        }
        public class TransferReq : BettingDataLC_Allbet
        {

        }

        [HttpGet]
        [Route("[controller]/v2/{method}/{username}")]
        public async Task<dynamic> GetBalanceAsync([FromRoute] string method, [FromRoute] string username)
        {
            try
            {
                if (!Enum.TryParse(method, true, out AllbetType transType))
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, $"Not Support {method} method.");
                }

                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Date", out var date);

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var signString = $"GET\n\n\n{date}\n/{method}/{username}";
                var signature = Sign(signString, thirdPartySetting.TpSetting["V2PartnerKey"]);
                _logger.LogInformation($"authorization={authorization}, date={date}, signString={signString}, signature={signature}");
                
                if (authorization != $"AB {thirdPartySetting.TpSetting["V2OperatorId"]}:{signature}")
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidSignature);
                }

                return await _AllbetBll.SeamlessWallet(transType, username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        [HttpPost]
        [Route("[controller]/v2/{method}")]
        public async Task<dynamic> TransferAsync([FromRoute] string method)
        {
            try
            {
                if (!Enum.TryParse(method, true, out AllbetType transType))
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, $"Not Support {method} method.");
                }

                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Accept", out var accept);
                Request.Headers.TryGetValue("Content-MD5", out var md5);
                Request.Headers.TryGetValue("Content-Type", out var contentType);
                Request.Headers.TryGetValue("Date", out var date);

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var signString = $"POST\n{md5}\n{contentType}\n{date}\n/Transfer";
                var signature = Sign(signString, thirdPartySetting.TpSetting["V2PartnerKey"]);
                _logger.LogInformation($"authorization={authorization}, accept={accept}, md5={md5}, contentType={contentType}, date={date}, signString={signString}, signature={signature}");

                if (authorization != $"AB {thirdPartySetting.TpSetting["V2OperatorId"]}:{signature}")
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidSignature);
                }

                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var bodyText = await reader.ReadToEndAsync();
                return await _AllbetBll.SeamlessWallet(transType, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        [HttpPost]
        [Route("[controller]/[action]")]
        public async Task<Resp> TransferAsync()/*[FromForm]Dictionary<string, string> transferReqq*/
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            var resp = new Resp
            {
                error_code = "50000",
                message = "server error"
            };

            TransferReq transferReq = null;
            try
            {
                transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
            }
            catch (Exception ex)
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Transfer", ex);
                return resp;
            }
            var transferType = AllbetTransferType.Bet;
            Enum.TryParse(transferReq.TransferType.ToString(), out transferType);

            Request.Headers.TryGetValue("Authorization", out var authorization);
            Request.Headers.TryGetValue("Accept", out var accept);
            Request.Headers.TryGetValue("Content-MD5", out var md5);
            Request.Headers.TryGetValue("Content-Type", out var cType);
            Request.Headers.TryGetValue("Date", out var dtDate);

            var propertyId = "";
            var sign = "";
            var sAuthorization = authorization.ToString();
            if (!string.IsNullOrEmpty(sAuthorization))
            {
                propertyId = sAuthorization.Substring(3, sAuthorization.IndexOf(":") - 3);
                sign = sAuthorization.Substring(sAuthorization.IndexOf(":") + 1);
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            if (propertyId != thirdPartySetting.TpSetting["PropertyId"])
            {
                resp.error_code = "10000";
                resp.message = "invalid propertyId";
                log2File.Error($"{propertyId} != {thirdPartySetting.TpSetting["PropertyId"]}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            var signString = $"POST\n{md5}\n{cType}\n{dtDate}\n/transfer";
            _logger.LogInformation($"{authorization}\r\n{accept}\r\n{md5}\r\n{cType}\r\n{dtDate}\r\n{signString}");

            if (Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) != sign)
            {
                resp.error_code = "10001";
                resp.message = "invalid signature";
                log2File.Error($"{sign} != {Sign(signString, thirdPartySetting.TpSetting["SHA1key"])}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            if (string.IsNullOrEmpty(transferReq.Client) || transferReq.TranId <= 0 || transferReq.Details == null)
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }
            if (transferType == AllbetTransferType.EventSettle && transferReq.Details.Any(bet => bet.EventRecordNum <= 0) || transferType != AllbetTransferType.EventSettle && transferReq.Details.Any(bet => bet.BetNum <= 0))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferReq.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);

            if (!(tpCurrency.TpCurrencyId.Equals(transferReq.Currency, StringComparison.OrdinalIgnoreCase) || (environment.IsDevelopment() && transferReq.Currency.Equals("CNY", StringComparison.OrdinalIgnoreCase))))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            try
            {
                if (transferType == AllbetTransferType.Bet)
                {
                    decimal amountSum = 0;
                    var valid = true;

                    if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferReq.Client);

                        if (tpCheck == null)
                        {
                            resp.error_code = "10005";
                            resp.message = "User is disable logons";
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error_code = "10005";
                            resp.message = "User is disable logons";
                        }
                        else
                        {
                            var result = bettingBLL.PlaceBetLC_Allbet(transferReq, out var tpAffterAmount);
                            if (result > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else if (tpAffterAmount < 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "10101";
                                resp.message = "Credit is not enough.";
                            }
                            else if (result == -1)
                            {
                                resp.error_code = "10007";
                                resp.message = "invalid status: transaction had already been cancelled/settled";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "50000";
                                resp.message = "server error";
                            }
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == AllbetTransferType.Settle)
                {
                    decimal amountSum = 0;
                    var valid = true;

                    if (transferReq.Details.Count > 1)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var result = settledBLL.PayoutLC_Allbet(transferReq, out var tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == 0)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                        else if (result == -1)
                        {
                            resp.error_code = "10007";
                            resp.message = "invalid status: transaction had already been cancelled/settled";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == AllbetTransferType.Cancel)
                {
                    decimal amountSum = 0;
                    var valid = true;
                    if (transferReq.OriginalTranId <= 0)
                    {
                        valid = false;
                    }
                    else if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var result = settledBLL.CancelBetLC_Allbet(transferReq, out var tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (result == -3)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == AllbetTransferType.ReSettle)
                {
                    decimal amountSum = 0;
                    var valid = true;

                    if (transferReq.Details.Count > 1)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var result = settledBLL.ReSettledLC_Allbet(transferReq, out var tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "10101";
                            resp.message = "Credit is not enough.";
                        }
                        else if (result == 0)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                        else if (result == -1)
                        {
                            resp.error_code = "10007";
                            resp.message = "invalid status: transaction had already been cancelled/settled";
                        }
                        else if (result == -2)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == AllbetTransferType.EventSettle)
                {
                    decimal amountSum = 0;
                    var valid = true;

                    if (transferReq.Details.Count > 1)
                    {
                        // 批量活动结算是无效和禁止的
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var result = settledBLL.RewardLC_Allbet(transferReq, out var tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                tpAffterAmount = tpCustBalance.Balance;
                            }
                            resp.balance = tpAffterAmount;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (result == 0)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                        else if (result == -1)
                        {
                            resp.error_code = "10007";
                            resp.message = "invalid status: transaction had already been cancelled/settled";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{transferType}_Transfer", ex);
                throw;
            }

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
            return resp;
        }

        [HttpPost]
        [Route("[controller]/[action]")]
        public async Task<Resp> TransferFixAsync()/*[FromForm]Dictionary<string, string> transferReqq*/
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            var resp = new Resp
            {
                error_code = "50000",
                message = "server error"
            };

            if (tools.GetClientIP(HttpContext) != "211.75.236.133" && tools.GetClientIP(HttpContext) != "220.130.219.4")
            {
                return resp;
            }

            TransferReq transferReq = null;
            try
            {
                transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
            }
            catch (Exception ex)
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Transfer", ex);
                return resp;
            }
            var transferType = AllbetTransferType.Bet;
            Enum.TryParse(transferReq.TransferType.ToString(), out transferType);

            if (string.IsNullOrEmpty(transferReq.Client) || transferReq.TranId <= 0 || transferReq.Details == null || transferReq.Details.Any(bet => bet.BetNum <= 0))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferReq.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);

            if (!(tpCurrency.TpCurrencyId.Equals(transferReq.Currency, StringComparison.OrdinalIgnoreCase) || (environment.IsDevelopment() && transferReq.Currency.Equals("CNY", StringComparison.OrdinalIgnoreCase))))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            try
            {
                if (transferType == AllbetTransferType.Cancel)
                {
                    decimal amountSum = 0;
                    var valid = true;
                    if (transferReq.OriginalTranId <= 0)
                    {
                        valid = false;
                    }
                    else if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var result = settledBLL.CancelBetLC_Allbet(transferReq, out var tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (result == -3)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{transferType}_Transfer", ex);
                throw;
            }

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
            return resp;
        }

        [HttpGet]
        [Route("[controller]/[action]/{client}")]
        public Resp get_balance(string client)
        {
            var resp = new Resp
            {
                error_code = "50000",
                message = "server error"
            };

            Request.Headers.TryGetValue("Authorization", out var authorization);
            Request.Headers.TryGetValue("Accept", out var accept);
            Request.Headers.TryGetValue("Date", out var dtDate);

            var propertyId = "";
            var sign = "";
            var sAuthorization = authorization.ToString();
            if (!string.IsNullOrEmpty(sAuthorization))
            {
                propertyId = sAuthorization.Substring(3, sAuthorization.IndexOf(":") - 3);
                sign = sAuthorization.Substring(sAuthorization.IndexOf(":") + 1);
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            if (propertyId != thirdPartySetting.TpSetting["PropertyId"])
            {
                resp.error_code = "10000";
                resp.message = "invalid propertyId";
                log2File.Error($"{propertyId} != {thirdPartySetting.TpSetting["PropertyId"]}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
                return resp;
            }

            var signString = $"GET\n\n\n{dtDate}\n/get_balance/{client}";
            //log2File.Info($"{Sign(signString, thirdPartySetting.TpSetting["SHA1key"])}\r\n{signString}\r\n{authorization}", $"{thisTp}_get_balance_Sign");

            if (Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) != sign)
            {
                resp.error_code = "10001";
                resp.message = "invalid signature";
                log2File.Error($"{sign} != {Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) }\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
                return resp;
            }

            try
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, client);
                if (tpCheck == null)
                {
                    resp.error_code = "10005";
                    resp.message = "User is disable logons";
                }
                else if (tpCheck.Locked)
                {
                    resp.error_code = "10005";
                    resp.message = "User is disable logons";
                }
                else
                {
                    //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client, gameType);
                    TpCustBalance tpCustBalance = null;
                    if (environment.IsDevelopment())
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, client, gameType);
                    }
                    else
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client, gameType);
                    }

                    resp.balance = tpCustBalance.Balance;
                    resp.error_code = "0";
                    resp.message = "success";
                }

            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Balance", ex);
            }

            //log2File.Info($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
            return resp;
        }

        private string Sign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Convert.FromBase64String(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));

            return Convert.ToBase64String(hash);
        }
    }
}