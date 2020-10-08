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
    [Route("[controller]")]
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

        #endregion

        #region Response

        public class GetSeamlessWalletResp
        {
            public string status { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string userId { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public decimal balance { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string balanceTs { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string desc { get; set; }
        }

        #endregion

        [HttpPost]
        public GetSeamlessWalletResp SeamlessWallet([FromForm] Req req)
        {
            GetSeamlessWalletResp resp = new GetSeamlessWalletResp();
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (req.key != thirdPartySetting.TpSetting["cert"])
            {
                resp.status = "9999";
                resp.desc = "fail";
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
                BettingDataCF_AWC bet = JsonConvert.DeserializeObject<BettingDataCF_AWC>(req.message);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, bet.userId);
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
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, bet.userId);
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
                    CancelBetDataCF_AWC cancelBet = JsonConvert.DeserializeObject<CancelBetDataCF_AWC>(req.message);

                    int result = bettingBLL.CancelBetCF_AWC(cancelBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, cancelBet.userId);
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
                    AdjustBetDataCF_AWC adjustBet = JsonConvert.DeserializeObject<AdjustBetDataCF_AWC>(req.message);

                    int result = bettingBLL.AdjustBetCF_AWC(adjustBet, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        resp.status = "0000";
                        resp.balance = tpAffterAmount;
                        resp.balanceTs = getCurrentGameTime();
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, adjustBet.userId);
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
                    VoidBetDataCF_AWC voidBet = JsonConvert.DeserializeObject<VoidBetDataCF_AWC>(req.message);

                    int result = bettingBLL.VoidBetCF_AWC(voidBet);
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
                    UnvoidBetDataCF_AWC unvoidBet = JsonConvert.DeserializeObject<UnvoidBetDataCF_AWC>(req.message);

                    int result = bettingBLL.UnvoidBetCF_AWC(unvoidBet);
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
                    SettleDataCF_AWC settle = JsonConvert.DeserializeObject<SettleDataCF_AWC>(req.message);
                    resp.status = "0000";
                    //int result = settledBLL.SettleCF_AWC(settle, out decimal tpAffterAmount);

                    //if (result > 0)
                    //{
                    //    resp.status = "0000";
                    //}
                    //else
                    //{
                    //    resp.status = "9999";
                    //    resp.desc = "fail";
                    //}

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
                    UnsettleDataCF_AWC unsettle = JsonConvert.DeserializeObject<UnsettleDataCF_AWC>(req.message);
                    resp.status = "0000";
                    //int result = settledBLL.UnsettleCF_AWC(unsettle, out decimal tpAffterAmount);

                    //if (result > 0)
                    //{
                    //    resp.status = "0000";
                    //}
                    //else
                    //{
                    //    resp.status = "9999";
                    //    resp.desc = "fail";
                    //}

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
                    VoidSettleDataCF_AWC voidSettle = JsonConvert.DeserializeObject<VoidSettleDataCF_AWC>(req.message);
                    resp.status = "0000";
                    //int result = settledBLL.VoidSettleCF_AWC(voidSettle, out decimal tpAffterAmount);

                    //if (result > 0)
                    //{
                    //    resp.status = "0000";
                    //}
                    //else
                    //{
                    //    resp.status = "9999";
                    //    resp.desc = "fail";
                    //}

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
                    UnvoidSettleDataCF_AWC unvoidSettle = JsonConvert.DeserializeObject<UnvoidSettleDataCF_AWC>(req.message);
                    resp.status = "0000";
                    //int result = settledBLL.UnvoidSettleCF_AWC(unvoidSettle, out decimal tpAffterAmount);

                    //if (result > 0)
                    //{
                    //    resp.status = "0000";
                    //}
                    //else
                    //{
                    //    resp.status = "9999";
                    //    resp.desc = "fail";
                    //}

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
                    RefundDataCF_AWC refund = JsonConvert.DeserializeObject<RefundDataCF_AWC>(req.message);
                    resp.status = "0000";
                    //int result = settledBLL.RefundCF_AWC(refund, out decimal tpAffterAmount);

                    //if (result > 0)
                    //{
                    //    resp.status = "0000";
                    //}
                    //else
                    //{
                    //    resp.status = "9999";
                    //    resp.desc = "fail";
                    //}

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
