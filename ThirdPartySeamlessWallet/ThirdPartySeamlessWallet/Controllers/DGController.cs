using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{

    [ApiController]
    public class DGController : ControllerBase
    {
        private readonly ILogger<DGController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;

        public DGController(ILogger<DGController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
        }
        public class Member
        {
            public string username { get; set; }
        }
        public class GetBalanceReq
        {
            public string token { get; set; }
            public Member member { get; set; }
        }

        public class MemberBalance : Member
        {
            public decimal balance { get; set; }
        }

        public class GetBalanceRep
        {
            public int codeId { get; set; }
            public string token { get; set; }
            public MemberBalance member { get; set; }
        }

        [Route("[controller]/user/getBalance/{agentName}")]
        [HttpPost]
        public GetBalanceRep getBalance(string agentName, [FromBody] GetBalanceReq getBalanceReq)
        {
            logger.LogInformation(JsonConvert.SerializeObject(getBalanceReq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);
            string token = tools.ToMD5($"{agentName}{thirdPartySetting.TpSetting["APIkey"]}").ToLower();
            GetBalanceRep getBalanceRep = new GetBalanceRep();
            getBalanceRep.member = new MemberBalance();
            getBalanceRep.member.username = getBalanceReq.member.username;
            getBalanceRep.token = token;
            getBalanceRep.codeId = (int)Error.SystemError;

            if (getBalanceReq.token != token)
            {
                getBalanceRep.codeId = (int)Error.TokenVerificationFailed;
                return getBalanceRep;
            }

            string username = getBalanceReq.member.username;

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.DG, username);
            if (tpCheck == null)
            {
                getBalanceRep.codeId = (int)Error.Thisaccounthasbeensignedout;
                return getBalanceRep;
            }
            else if (tpCheck.Locked)
            {
                getBalanceRep.codeId = (int)Error.Accountislocked;
                return getBalanceRep;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.DG, username);
            getBalanceRep.member.balance = tpCustBalance.Balance;
            getBalanceRep.codeId = (int)Error.OperationSuccessful;
            return getBalanceRep;
        }

        public class MemberTransfer : Member
        {
            public decimal amount { get; set; }
        }
        public class TransferReq
        {
            public string token { get; set; }
            public long ticketId { get; set; }
            public MemberTransfer member { get; set; }
            public string data { get; set; }
        }
        public class MemberTransfer2 : MemberTransfer
        {
            public decimal balance { get; set; }
        }

        public class TransferRep
        {
            public int codeId { get; set; }
            public string token { get; set; }
            public string data { get; set; }
            public MemberTransfer2 member { get; set; }
        }

        [Route("[controller]/account/transfer/{agentName}")]
        [HttpPost]
        public async Task<TransferRep> transferAsync(string agentName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyText = await reader.ReadToEndAsync();
            TransferReq transferReq = JsonConvert.DeserializeObject<TransferReq>(bodyText);
            log2File.Info(bodyText, "transfer");
            logger.LogInformation(JsonConvert.SerializeObject(transferReq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);
            string token = tools.ToMD5($"{agentName}{thirdPartySetting.TpSetting["APIkey"]}").ToLower();
            TransferRep transferRep = new TransferRep();
            transferRep.member = new MemberTransfer2();
            transferRep.member.username = transferReq.member.username;
            transferRep.member.amount = transferReq.member.amount;
            transferRep.token = token;
            transferRep.codeId = (int)Error.SystemError;
            try
            {
                if (transferReq.token != token)
                {
                    transferRep.codeId = (int)Error.TokenVerificationFailed;
                    return transferRep;
                }

                string username = transferReq.member.username;
                if (transferReq.member.amount < 0)
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.DG, username);
                    if (tpCheck == null)
                    {
                        transferRep.codeId = (int)Error.Thisaccounthasbeensignedout;
                        return transferRep;
                    }
                    else if (tpCheck.Locked)
                    {
                        transferRep.codeId = (int)Error.Accountislocked;
                        return transferRep;
                    }
                }

                if (transferReq.member.amount < 0)
                {
                    decimal tpAffterAmount = 0;
                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();
                    if (bettingBLL.PlaceBetLC_DG(bettingDataLC_DG, out tpAffterAmount) > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.DG, username);
                        transferRep.codeId = (int)Error.Insufficientbalance;
                    }
                    else
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.DG, username);
                        transferRep.codeId = (int)Error.Transferfailed;
                    }
                }
                else if (transferReq.member.amount > 0)
                {
                    decimal tpAffterAmount = 0;
                    var result = settledBLL.Win((int)Tp.DG, transferReq.ticketId.ToString(), transferReq.member.username, transferReq.member.amount, DateTimeOffset.Now.ToUnixTimeSeconds(), GameType.DG_LiveCasino, out tpAffterAmount);

                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
                else
                {
                    decimal tpAffterAmount = 0;
                    var result = settledBLL.Lost((int)Tp.DG, transferReq.ticketId.ToString(), transferReq.member.username, DateTimeOffset.Now.ToUnixTimeSeconds(), GameType.DG_LiveCasino, out tpAffterAmount);
                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;

                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.DG, transferReq.member.username);
                        transferRep.member.balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return transferRep;
        }

        [Route("[controller]/account/inform/{agentName}")]
        [HttpPost]
        public async Task<TransferRep> informAsync(string agentName)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyText = await reader.ReadToEndAsync();
            TransferReq transferReq = JsonConvert.DeserializeObject<TransferReq>(bodyText);
            log2File.Info(bodyText, "inform");
            logger.LogInformation(JsonConvert.SerializeObject(transferReq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);
            string token = tools.ToMD5($"{agentName}{thirdPartySetting.TpSetting["APIkey"]}").ToLower();
            TransferRep transferRep = new TransferRep();
            transferRep.member = new MemberTransfer2();
            transferRep.member.username = transferReq.member.username;
            transferRep.member.amount = transferReq.member.amount;
            transferRep.token = token;
            transferRep.codeId = (int)Error.SystemError;
            try
            {
                if (transferReq.token != token)
                {
                    transferRep.codeId = (int)Error.TokenVerificationFailed;
                    return transferRep;
                }

                if (transferReq.member.amount < 0)
                {
                    decimal tpAffterAmount = 0;
                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();
                    var result = bettingBLL.CancelBetLC_DG(bettingDataLC_DG, out tpAffterAmount);
                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else if (result == -1)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.DG, transferReq.member.username);
                        transferRep.member.balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        //var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.DG, username);
                        transferRep.codeId = (int)Error.Transferfailed;
                    }
                }
                else if (transferReq.member.amount > 0)
                {
                    decimal tpAffterAmount = 0;
                    var result = settledBLL.Win((int)Tp.DG, transferReq.ticketId.ToString(), transferReq.member.username, transferReq.member.amount, DateTimeOffset.Now.ToUnixTimeSeconds(), GameType.DG_LiveCasino, out tpAffterAmount);

                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
                else
                {
                    decimal tpAffterAmount = 0;
                    var result = settledBLL.Lost((int)Tp.DG, transferReq.ticketId.ToString(), transferReq.member.username, DateTimeOffset.Now.ToUnixTimeSeconds(), GameType.DG_LiveCasino, out tpAffterAmount);
                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = token;

                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)Tp.DG, transferReq.member.username);
                        transferRep.member.balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return transferRep;
        }
        public enum Error
        {
            OperationSuccessful = 0,
            ParameterError1 = 1,
            TokenVerificationFailed = 2,
            CommandNotFind = 3,
            IllegalOperation = 4,
            Dateformaterror = 10,
            Dataformaterror = 11,
            Permissiondenied = 97,
            Operationfailed = 98,
            UnknownError = 99,
            Accountislocked = 100,
            Accountformaterror = 101,
            Accountdoesnotexist = 102,
            Thisaccountistaken = 103,
            Passwordformaterror = 104,
            Passwordwrong = 105,
            New_OldPasswordisthesame = 106,
            Memberaccountunavailable = 107,
            LoginError = 108,
            SignupError = 109,
            Thisaccounthasbeensignedin = 110,
            Thisaccounthasbeensignedout = 111,
            Thisaccountisnotsignedin = 112,
            TheAgentaccountinputtedisnotanAgentaccount = 113,
            Membernotfound = 114,
            Accountoccupied = 116,
            Cannotfindbranchofmember = 117,
            CannotfindthespecifiedAgent = 118,
            InsufficentfundsduringAgentwithdrawal = 119,
            Insufficientbalance = 120,
            Profitlimitmustbegreaterthanorequalto0 = 121,
            Ranoutoffreedemoaccounts = 150,
            systemmaintenance = 300,
            WrongAPIkey = 320,
            LimitGroupNotFound = 321,
            CurrencyNameNotFound = 322,
            UseserialnumbersforTransfer = 323,
            Transferfailed = 324,
            AgentStatusUnavailable = 325,
            MembersAgentNovideogroup = 326,
            ClientIPRestricted = 400,
            Networklatency = 401,
            Theconnectionisclosed = 402,
            Clientslimitedsources = 403,
            Resourcerequesteddoesnotexist = 404,
            Toofrequentrequests = 405,
            Requesttimedout = 406,
            Cannotfindgameaddress = 407,
            Nullpointerexception = 500,
            SystemError = 501,
            Thesystemisbusy = 502,
            Dataoperationerror = 503
        }
    }
}