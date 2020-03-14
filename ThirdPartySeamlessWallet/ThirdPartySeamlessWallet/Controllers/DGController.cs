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
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{

    [ApiController]
    public class DGController : ControllerBase
    {
        private readonly ILogger<SAController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;
        private readonly TpCustInfoBLL tpCustInfoBLL;

        public DGController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools)
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.tpCustInfoBLL = tpCustInfoBLL;
        }

        public class Member
        {
            public string username { get; set; }
        }
        public class WalletReq
        {
            public string token { get; set; }
            public Member member { get; set; }
        }

        public class MemberBalance : Member
        {
            public decimal balance { get; set; }
        }

        public class GetBalance
        {
            public int codeId { get; set; }
            public string token { get; set; }
            public MemberBalance member { get; set; }
        }

        [Route("[controller]/user/getBalance/{agentName}")]
        [HttpPost]
        public async Task<GetBalance> getBalanceAsync(string agentName, [FromBody] WalletReq walletReq)
        {
            logger.LogInformation(JsonConvert.SerializeObject(walletReq));
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            logger.LogInformation(Content);
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);
            string token = tools.ToMD5($"{agentName}{thirdPartySetting.TpSetting["APIkey"]}").ToLower();
            GetBalance getBalance = new GetBalance();
            getBalance.member = new MemberBalance();
            getBalance.member.username = walletReq.member.username;
            getBalance.token = token;
            getBalance.codeId = (int)Error.SystemError;

            if (walletReq.token != token)
            {
                getBalance.codeId = (int)Error.TokenVerificationFailed;
                return getBalance;
            }

            string username = walletReq.member.username;

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.DG, username);
            if (tpCheck == null)
            {
                getBalance.codeId = (int)Error.Accountislocked;
                return getBalance;
            }
            else if (tpCheck.Locked)
            {
                getBalance.codeId = (int)Error.Accountislocked;
                return getBalance;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.DG, username);
            getBalance.member.balance = tpCustBalance.Balance;
            getBalance.codeId = (int)Error.OperationSuccessful;
            return getBalance;
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