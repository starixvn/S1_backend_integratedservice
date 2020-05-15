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
    public class AllbetController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;

        public enum TransferType : int
        {
            Bet = 10,
            Cancel = 11,
            Settle = 20,
            ReSettle = 21
        }

        public AllbetController(IHostEnvironment environment, BettingBLL bettingBLL, ThirdPartyBLL thirdPartyBLL, SettledBLL settledBLL, TpCustInfoBLL tpCustInfoBLL, Log2File log2File)
        {
            this.log2File = log2File;
            thisTp = Tp.Allbet;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.environment = environment;
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

        [HttpPost]
        [Route("[controller]/[action]")]
        public async Task<Resp> TransferAsync()/*[FromForm]Dictionary<string, string> transferReqq*/
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

            var transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
            TransferType transferType = TransferType.Bet;
            Enum.TryParse(transferReq.TransferType.ToString(), out transferType);

            try
            {
                if (transferType == TransferType.Bet)
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
                        if (bettingBLL.PlaceBetLC_Allbet(transferReq, out decimal tpAffterAmount) > 0)
                        {
                            if (transferReq.Amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                                resp.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferReq.Client);
                                resp.balance = tpAffterAmount;

                            }

                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "10101";
                            resp.message = "Credit is not enough.";
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "50000";
                            resp.message = "server error";
                        }
                    }
                }
                else if (transferType == TransferType.Settle)
                {
                    int result = settledBLL.SettledLC_Allbet(transferReq, out decimal tpAffterAmount);

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
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                        resp.balance = tpCustBalance.Balance;
                        resp.error_code = "0";
                        resp.message = "success";
                    }
                }
                else if (transferType == TransferType.Cancel)
                {
                    int result = bettingBLL.CancelBetLC_Allbet(transferReq, out decimal tpAffterAmount);

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
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                        resp.balance = tpCustBalance.Balance;
                        resp.error_code = "0";
                        resp.message = "success";
                    }
                }
                else if (transferType == TransferType.ReSettle)
                {
                    int result = settledBLL.ReSettledLC_Allbet(transferReq, out decimal tpAffterAmount);

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
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                        resp.balance = tpCustBalance.Balance;
                        resp.error_code = "10101";
                        resp.message = "Credit is not enough.";
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client);
                        resp.balance = tpCustBalance.Balance;
                        resp.error_code = "0";
                        resp.message = "success";
                    }
                }
            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{transferType}_Transfer");
                throw ex;
            }

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
            return resp;
        }

        [HttpGet]
        [Route("[controller]/[action]/{client}")]
        public Resp get_balance(string client)
        {
            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

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
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client);
                    resp.balance = tpCustBalance.Balance;
                    resp.error_code = "0";
                    resp.message = "success";
                }

            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Balance");
            }

            //log2File.Info($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
            return resp;
        }
    }
}