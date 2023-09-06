using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class WMController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tp thisTp;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly IHostEnvironment environment;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly int gameType = 0;

        public WMController(
            IHostEnvironment environment,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            AuthBLL authBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL
        )
        {
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.environment = environment;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            thisTp = Tp.WM;
            gameType = (int)GameType.WM_LiveCasino;
        }

        #region Request

        public class Req
        {
            public string cmd { get; set; }
            public string signature { get; set; }
            public DateTime requestDate { get; set; }
            public string user { get; set; }
        }

        #endregion

        #region Response

        public class GetSeamlessWalletResp
        {
            public int errorCode { get; set; }
            public string errorMessage { get; set; }
        }

        public class CallBalanceResp : GetSeamlessWalletResp
        {
            public BalanceInfo result { get; set; }
        }

        public class BalanceInfo
        {
            public string user { get; set; }
            public decimal money { get; set; }
            public string responseDate { get; set; }
        }

        public class PointInoutResp : GetSeamlessWalletResp
        {
            public PointInoutInfo result { get; set; }
        }

        public class PointInoutInfo
        {
            public decimal money { get; set; }
            public string responseDate { get; set; }
            public string dealid { get; set; }
            public decimal cash { get; set; }
        }

        #endregion

        [Route("[controller]")]
        [HttpPost]
        public GetSeamlessWalletResp SeamlessWallet([FromForm] Dictionary<string, string> req)
        {
            var Content = JsonConvert.SerializeObject(req);
            Req getReq = JsonConvert.DeserializeObject<Req>(Content);
            string logName = $"{thisTp}_{getReq.cmd}";

            GetSeamlessWalletResp resp = new GetSeamlessWalletResp();
            resp.errorCode = 1;

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            if (getReq.signature == null)
            {
                resp.errorCode = 10304;
                resp.errorMessage = "signature empty";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_signature");
                return resp;
            }

            var getTpCurrency = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.user, gameType);
            string currency = getTpCurrency.TpCurrencyId;
            if (getReq.signature != thirdPartySetting.TpSetting[$"signature_{currency}"])
            {
                resp.errorCode = 10303;
                resp.errorMessage = "signature error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_signature");
                return resp;
            }

            if (string.Equals(getReq.cmd, "CallBalance", StringComparison.OrdinalIgnoreCase))
            {
                CallBalanceResp callBalanceResp = new CallBalanceResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new BalanceInfo
                    {
                        user = getReq.user,
                        responseDate = getCurrentGameTime()
                    }
                };

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.user);
                if (tpCheck == null)
                {
                    callBalanceResp.errorCode = 10501;
                    callBalanceResp.errorMessage = "Invalid user Id";
                    log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                if (tpCheck.Locked)
                {
                    callBalanceResp.errorCode = 10505;
                    callBalanceResp.errorMessage = "Account is Lock";
                    log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.user, gameType);
                if (tpCustBalance != null)
                {
                    callBalanceResp.errorCode = 0;
                    callBalanceResp.errorMessage = "";
                    callBalanceResp.result.money = tpCustBalance.Balance;
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                    return callBalanceResp;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(callBalanceResp)}", logName);
                return callBalanceResp;
            }
            else if (string.Equals(getReq.cmd, "PointInout", StringComparison.OrdinalIgnoreCase))
            {
                BettingDataLC_WM getPointInoutReq = JsonConvert.DeserializeObject<BettingDataLC_WM>(Content);
                PointInoutResp pointInoutResp = new PointInoutResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new PointInoutInfo
                    {
                        money = getPointInoutReq.money,
                        dealid = getPointInoutReq.dealid,
                        responseDate = getCurrentGameTime(),
                    }
                };

                var result = 0;
                switch (getPointInoutReq.code)
                {
                    case "1":
                        // 加點
                        if (environment.IsDevelopment() && getPointInoutReq.money >= 30m)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        try
                        {
                            result = settledBLL.PayoutLC_WM(getPointInoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                //var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getPointInoutReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getPointInoutReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "2":
                        // 扣點
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getPointInoutReq.user);
                        if (tpCheck == null)
                        {
                            pointInoutResp.errorCode = 10501;
                            pointInoutResp.errorMessage = "Invalid user Id";
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        if (tpCheck.Locked)
                        {
                            pointInoutResp.errorCode = 10505;
                            pointInoutResp.errorMessage = "Account is Lock";
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        if (environment.IsDevelopment() && getPointInoutReq.money == -40m)
                        {
                            System.Threading.Thread.Sleep(60000);
                            //log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            //return pointInoutResp;
                        }
                        try
                        {
                            result = bettingBLL.PlaceBetLC_WM(getPointInoutReq, out decimal tpAffterAmount);

                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getPointInoutReq.user);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (tpAffterAmount < 0)
                            {
                                pointInoutResp.errorCode = 10805;
                                pointInoutResp.errorMessage = "Not Enough Balance";
                                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "3":
                    case "4":
                    case "5":
                        try
                        {
                            //  3:重對加點 4:重對扣點 5:重新派彩
                            result = settledBLL.ResettleLC_WM(getPointInoutReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getPointInoutReq.user);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    default:
                        break;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                return pointInoutResp;
            }
            else if (string.Equals(getReq.cmd, "TimeoutBetReturn", StringComparison.OrdinalIgnoreCase))
            {
                BettingDataLC_WM getTimeoutBetReturnReq = JsonConvert.DeserializeObject<BettingDataLC_WM>(Content);
                PointInoutResp pointInoutResp = new PointInoutResp
                {
                    errorCode = 1,
                    errorMessage = "fail",
                    result = new PointInoutInfo
                    {
                        money = getTimeoutBetReturnReq.money,
                        dealid = getTimeoutBetReturnReq.dealid,
                        responseDate = getCurrentGameTime(),
                    }
                };
                int result = 0;
                switch (getTimeoutBetReturnReq.code)
                {
                    case "1":
                        if (environment.IsDevelopment() && getTimeoutBetReturnReq.money >= 100m)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                            return pointInoutResp;
                        }
                        try
                        {
                            // 加點
                            result = settledBLL.PayoutLC_WM(getTimeoutBetReturnReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getTimeoutBetReturnReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    case "2":
                        try
                        {
                            // 扣點
                            result = settledBLL.CancelBetLC_WM(getTimeoutBetReturnReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                //var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getTimeoutBetReturnReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpAffterAmount;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                            else if (result == -1)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getTimeoutBetReturnReq.user, gameType);
                                pointInoutResp.errorCode = 0;
                                pointInoutResp.errorMessage = "";
                                pointInoutResp.result.cash = tpCustBalance.Balance;
                                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                                return pointInoutResp;
                            }
                        }
                        catch (Exception ex)
                        {
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}\r\n{ex.Message}", logName, ex);
                            return pointInoutResp;
                        }
                        break;
                    default:
                        break;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(pointInoutResp)}", logName);
                return pointInoutResp;
            }

            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        private string getCurrentGameTime()
        {
            return new DateTimeOffset(DateTime.Now).ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
