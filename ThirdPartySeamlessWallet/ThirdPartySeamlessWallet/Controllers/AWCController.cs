using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{

    [ApiController]
    public class AWCController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<AWCController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public AWCController(
            IHostEnvironment environment,
            ILogger<AWCController> logger,
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            Log2File log2File
        )
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.thisTp = Tp.AWC;
            gameType = (int)GameType.AWC_Rooster;
            this.environment = environment;
        }

        #region Request

        public class Req
        {
            public string key { get; set; }
            public string message { get; set; }
        }

        public class ReqAction
        {
            public string action { get; set; }
        }

        public class GetBalanceReq
        {
            public string action { get; set; }
            public string userId { get; set; }
        }

        #endregion

        #region Response

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        public class GetSeamlessWalletResp
        {
            public string status { get; set; }
            public string userId { get; set; }
            public decimal balance { get; set; }
            public string balanceTs { get; set; }
            public string desc { get; set; }
        }

        #endregion

        [Route("[controller]")]
        [HttpPost]
        public GetSeamlessWalletResp SeamlessWallet([FromForm] Req req)
        {
            GetSeamlessWalletResp resp = new GetSeamlessWalletResp();

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "9999";
                resp.desc = "fail";
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_cert");
                return resp;
            }

            ReqAction reqAction = JsonConvert.DeserializeObject<ReqAction>(req.message);
            string logName = $"{thisTp}_{reqAction.action}";
            if (string.Equals(reqAction.action, "getBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceReq getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(req.message);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.userId);
                if (tpCheck == null)
                {
                    resp.status = "1000";
                    resp.desc = "Invalid user Id";
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCheck.Locked)
                {
                    resp.status = "1013";
                    resp.desc = "Account is Lock";
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.userId, gameType);
                if (tpCustBalance != null)
                {
                    resp.status = "0000";
                    resp.userId = getBalanceReq.userId;
                    resp.balance = tpCustBalance.Balance;
                    resp.balanceTs = getCurrentGameTime();
                }
                else
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                }
                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            else if (string.Equals(reqAction.action, "bet", StringComparison.OrdinalIgnoreCase))
            {
                BetCF_AWC bet = JsonConvert.DeserializeObject<BetCF_AWC>(req.message);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, bet.txns.ElementAt(0).userId);
                if (tpCheck == null)
                {
                    resp.status = "1012";
                    resp.desc = "Invalid user Id";
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCheck.Locked)
                {
                    resp.status = "1013";
                    resp.desc = "Account is Lock";
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                try
                {
                    int result = bettingBLL.PlaceBetCF_AWC(bet, out decimal tpAffterAmount);
                    if (environment.IsDevelopment() && bet.txns.ElementAt(0).betAmount == 40m)
                    {
                        System.Threading.Thread.Sleep(60000);
                    }
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.txns.ElementAt(0).userId);
                        resp.status = "0000";
                        resp.balance = tpCustBalance.Balance;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.status = "1018";
                        resp.desc = "Not Enough Balance";
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "cancelBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    CancelCF_AWC cancelBet = JsonConvert.DeserializeObject<CancelCF_AWC>(req.message);

                    int result = bettingBLL.CancelBetCF_AWC(cancelBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelBet.txns.ElementAt(0).userId);
                        resp.status = "0000";
                        resp.balance = tpCustBalance.Balance;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "adjustBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    AdjustBetCF_AWC adjustBet = JsonConvert.DeserializeObject<AdjustBetCF_AWC>(req.message);

                    int result = bettingBLL.AdjustBetCF_AWC(adjustBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, adjustBet.txns.ElementAt(0).userId);
                        resp.status = "0000";
                        resp.balance = tpCustBalance.Balance;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "voidBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidBetCF_AWC voidBet = JsonConvert.DeserializeObject<VoidBetCF_AWC>(req.message);

                    int result = bettingBLL.VoidBetCF_AWC(voidBet);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "unvoidBet", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnVoidBetCF_AWC unvoidBet = JsonConvert.DeserializeObject<UnVoidBetCF_AWC>(req.message);

                    int result = bettingBLL.UnvoidBetCF_AWC(unvoidBet);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "settle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    PayoutCF_AWC payout = JsonConvert.DeserializeObject<PayoutCF_AWC>(req.message);

                    int result = settledBLL.PayoutCF_AWC(payout, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "unsettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnPayoutCF_AWC unpayout = JsonConvert.DeserializeObject<UnPayoutCF_AWC>(req.message);

                    int result = settledBLL.UnPayoutCF_AWC(unpayout, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "voidSettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    VoidSettleCF_AWC voidSettle = JsonConvert.DeserializeObject<VoidSettleCF_AWC>(req.message);

                    int result = settledBLL.VoidSettleCF_AWC(voidSettle, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "unvoidSettle", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    UnVoidSettleCF_AWC unvoidSettle = JsonConvert.DeserializeObject<UnVoidSettleCF_AWC>(req.message);

                    int result = settledBLL.UnvoidSettleCF_AWC(unvoidSettle, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                    }
                    else if (result == -1)
                    {
                        resp.status = "0000";
                    }
                    else
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }

                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }
            else if (string.Equals(reqAction.action, "refund", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RefundCF_AWC refund = JsonConvert.DeserializeObject<RefundCF_AWC>(req.message);
                    int cnt = refund.txns.Count();
                    if (cnt % 2 != 0)
                    {
                        resp.status = "9999";
                        resp.desc = "fail";
                    }
                    else
                    {
                        Dictionary<string, RefundCF_AWC> keyValuePairs = new Dictionary<string, RefundCF_AWC>();
                        foreach (var item in refund.txns)
                        {
                            RefundCF_AWC refRefundList;
                            if (keyValuePairs.ContainsKey(item.refundPlatformTxId))
                            {
                                refRefundList = keyValuePairs[item.refundPlatformTxId];
                            }
                            else
                            {
                                refRefundList = new RefundCF_AWC();
                                keyValuePairs.Add(item.refundPlatformTxId, refRefundList);
                                refRefundList.txns = new List<RefundDataCF_AWC>();
                            }
                            refRefundList.txns.Add(item);
                        }

                        var segment = keyValuePairs.Values.ToList();
                        foreach (var record in segment)
                        {
                            int result = settledBLL.RefundCF_AWC(record, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                resp.status = "0000";
                            }
                            else if (result == -1)
                            {
                                resp.status = "0000";
                            }
                            else
                            {
                                resp.status = "9999";
                                resp.desc = "fail";
                                log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                                return resp;
                            }
                        }
                    }
                    log2File.Info($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                catch (Exception ex)
                {
                    resp.status = "9999";
                    resp.desc = "fail";
                    log2File.Error($"{JsonConvert.SerializeObject(req)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName);
                }
            }

            return resp;
        }

        private string getCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

    }
}
