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
        private readonly string tpGameName;
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
            tpGameName = "AWC";
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

        public class GetBetReq
        {
            public IList<BettingDataCF_AWC> txns { get; set; }
        }

        public class GetCancelReq
        {
            public IList<CancelBetDataCF_AWC> txns { get; set; }
        }

        public class GetAdjustBetReq
        {
            public IList<AdjustBetDataCF_AWC> txns { get; set; }
        }

        public class GetVoidBetReq
        {
            public IList<VoidBetDataCF_AWC> txns { get; set; }
        }

        public class GetUnVoidBetReq
        {
            public IList<UnvoidBetDataCF_AWC> txns { get; set; }
        }

        public class GetSettleReq
        {
            public IList<SettleDataCF_AWC> txns { get; set; }
        }

        public class GetUnSettleReq
        {
            public IList<UnsettleDataCF_AWC> txns { get; set; }
        }

        public class GetVoidSettleReq
        {
            public IList<VoidSettleDataCF_AWC> txns { get; set; }
        }

        public class GetUnVoidSettleReq
        {
            public IList<UnvoidSettleDataCF_AWC> txns { get; set; }
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
                GetBetReq bet = JsonConvert.DeserializeObject<GetBetReq>(req.message);

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
                    int result = bettingBLL.PlaceBetCF_AWC(bet.txns.ElementAt(0), out decimal tpAffterAmount);
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
                    GetCancelReq cancelBet = JsonConvert.DeserializeObject<GetCancelReq>(req.message);

                    int result = bettingBLL.CancelBetCF_AWC(cancelBet.txns.ElementAt(0), out decimal tpAffterAmount);
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
                    GetAdjustBetReq adjustBet = JsonConvert.DeserializeObject<GetAdjustBetReq>(req.message);

                    int result = bettingBLL.AdjustBetCF_AWC(adjustBet.txns.ElementAt(0), out decimal tpAffterAmount);
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
                    GetVoidBetReq voidBet = JsonConvert.DeserializeObject<GetVoidBetReq>(req.message);

                    int result = bettingBLL.VoidBetCF_AWC(voidBet.txns.ElementAt(0));
                    if (result > 0)
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
                    GetUnVoidBetReq unvoidBet = JsonConvert.DeserializeObject<GetUnVoidBetReq>(req.message);

                    int result = bettingBLL.UnvoidBetCF_AWC(unvoidBet.txns.ElementAt(0));
                    if (result > 0)
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
                    GetSettleReq settle = JsonConvert.DeserializeObject<GetSettleReq>(req.message);

                    int result = settledBLL.PayoutCF_AWC(settle.txns.ElementAt(0), out decimal tpAffterAmount);
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
                    GetUnSettleReq unsettle = JsonConvert.DeserializeObject<GetUnSettleReq>(req.message);

                    int result = settledBLL.UnPayoutCF_AWC(unsettle.txns.ElementAt(0), out decimal tpAffterAmount);
                    if (result > 0)
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
                    GetVoidSettleReq voidSettle = JsonConvert.DeserializeObject<GetVoidSettleReq>(req.message);

                    int result = settledBLL.VoidSettleCF_AWC(voidSettle.txns.ElementAt(0), out decimal tpAffterAmount);
                    if (result > 0)
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
                    GetUnVoidSettleReq unvoidSettle = JsonConvert.DeserializeObject<GetUnVoidSettleReq>(req.message);

                    int result = settledBLL.UnvoidSettleCF_AWC(unvoidSettle.txns.ElementAt(0), out decimal tpAffterAmount);
                    if (result > 0)
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
                    int result = settledBLL.RefundCF_AWC(refund, out decimal tpAffterAmount);

                    if (result > 0)
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

            return resp;
        }

        private string getCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffK");
        }

    }
}
