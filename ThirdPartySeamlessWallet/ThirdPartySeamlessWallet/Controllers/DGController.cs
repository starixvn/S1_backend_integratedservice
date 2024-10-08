using System.Text;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<DGController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Tools _tools;
        private readonly Log2File _log2File;
        private readonly int _gameType = 0;
        private readonly int _thisTp;
        private readonly string _tpName = Tp.DG.ToString();
        private readonly DGConfig _defaultConfig; // SiteConfig的SiteId=0
        private readonly Dictionary<string, DGConfig> _siteNameConfigs;

        public DGController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<DGController>>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _log2File = provider.GetRequiredService<Log2File>();
            _gameType = (int)GameType.DG_LiveCasino;
            _thisTp = (int)Tp.DG;
            // defaultConfig給原本NOVA、GOAL、BWING的商號使用
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<DGConfig>(_tpName);
            // 新增商號使用指定siteName
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<DGConfig>(_tpName);

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

        #region Request

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

        public class CheckTransferReq
        {
            public string token { get; set; }
            public string data { get; set; }
        }

        #endregion

        #region Response

        public class GetBalanceRep
        {
            public int codeId { get; set; }
            public string token { get; set; }
            public MemberBalance member { get; set; }
        }

        public class TransferRep
        {
            public int codeId { get; set; }
            public string token { get; set; }
            public string data { get; set; }
            public MemberTransfer2 member { get; set; }
        }

        public class CheckTransferRep
        {
            public string token { get; set; }
            public int codeId { get; set; }
        }

        #endregion

        [Route("[controller]/user/getBalance/{agentName}")]
        [Route("[controller]/{siteName}/user/getBalance/{agentName}")]
        [HttpPost]
        public GetBalanceRep GetBalance(string agentName, [FromBody] GetBalanceReq getBalanceReq,string siteName = "")
        {
            GetBalanceRep getBalanceRep = new GetBalanceRep();
            getBalanceRep.member = new MemberBalance();
            getBalanceRep.member.username = getBalanceReq.member.username;
            getBalanceRep.token = getBalanceReq.token;
            getBalanceRep.codeId = (int)Error.SystemError;
            
            if (!CheckToken(agentName,getBalanceReq.token,siteName))
            {
                getBalanceRep.codeId = (int)Error.TokenVerificationFailed;
                return getBalanceRep;
            }

            string username = getBalanceReq.member.username;
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, username);
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

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
            getBalanceRep.member.balance = tpCustBalance.Balance;
            getBalanceRep.codeId = (int)Error.OperationSuccessful;
            return getBalanceRep;
        }

        [Route("[controller]/account/transfer/{agentName}")]
        [Route("[controller]/{siteName}/account/transfer/{agentName}")]
        [HttpPost]
        public async Task<TransferRep> TransferAsync(string agentName,string siteName = "")
        {
            string logName = $"{_tpName}_Transfer";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyText = await reader.ReadToEndAsync();

            TransferReq transferReq = JsonConvert.DeserializeObject<TransferReq>(bodyText);
            _logger.LogInformation(JsonConvert.SerializeObject(transferReq));

            TransferRep transferRep = new TransferRep();
            transferRep.member = new MemberTransfer2();
            transferRep.member.username = transferReq.member.username;
            transferRep.member.amount = transferReq.member.amount;
            transferRep.token = transferReq.token;
            transferRep.codeId = (int)Error.SystemError;

            try
            {
                if (!CheckToken(agentName,transferReq.token,siteName))
                {
                    transferRep.codeId = (int)Error.TokenVerificationFailed;
                    return transferRep;
                }

                string username = transferReq.member.username;
                if (transferReq.member.amount < 0)
                {
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, username);
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
                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    if (_bettingBLL.PlaceBetLC_DG(bettingDataLC_DG, out decimal tpAffterAmount) > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        transferRep.codeId = (int)Error.Insufficientbalance;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.Transferfailed;
                    }
                }
                else if (transferReq.member.amount > 0)
                {
                    if (transferReq.data != transferReq.ticketId.ToString())/*avoid DG Wrong Settled*/
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                        return transferRep;
                    }

                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    var result = _settledBLL.PayoutLC_DG(bettingDataLC_DG, out decimal tpAffterAmount);
                    if (result > 0 || result == -1/*has been Settled*/)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;
                        transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
                else
                {
                    if (transferReq.data != transferReq.ticketId.ToString())/*avoid DG Wrong Settled*/
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                        return transferRep;
                    }

                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    var result = _settledBLL.PayoutLC_DG(bettingDataLC_DG, out decimal tpAffterAmount);
                    if (result > 0 || result == -1/*has been Settled*/)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }

                _log2File.Info($"{bodyText}\r\n{JsonConvert.SerializeObject(transferRep)}", logName);
            }
            catch (Exception ex)
            {
                transferRep.codeId = (int)Error.SystemError;
                _log2File.Error($"{bodyText}\r\n{JsonConvert.SerializeObject(transferRep)}\r\n{ex.Message}", logName, ex);
            }

            return transferRep;
        }

        [Route("[controller]/account/inform/{agentName}")]
        [Route("[controller]/{siteName}/account/inform/{agentName}")]
        [HttpPost]
        public async Task<TransferRep> InformAsync(string agentName,string siteName = "")
        {
            string logName = $"{_tpName}_Inform";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyText = await reader.ReadToEndAsync();

            TransferReq transferReq = JsonConvert.DeserializeObject<TransferReq>(bodyText);
            _logger.LogInformation(JsonConvert.SerializeObject(transferReq));
            
            TransferRep transferRep = new TransferRep();
            transferRep.member = new MemberTransfer2();
            transferRep.member.username = transferReq.member.username;
            transferRep.member.amount = transferReq.member.amount;
            transferRep.token = transferReq.token;
            transferRep.codeId = (int)Error.SystemError;

            try
            {
                if (!CheckToken(agentName,transferReq.token,siteName))
                {
                    transferRep.codeId = (int)Error.TokenVerificationFailed;
                    return transferRep;
                }

                if (transferReq.member.amount < 0)
                {
                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    var result = _settledBLL.CancelBetLC_DG(bettingDataLC_DG, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;
                        transferRep.member.balance = tpAffterAmount - Math.Abs(transferReq.member.amount);
                    }
                    else if (result == -1)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance - Math.Abs(transferReq.member.amount);
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.Transferfailed;
                    }
                }
                else if (transferReq.member.amount > 0)
                {
                    if (transferReq.data != transferReq.ticketId.ToString())/*avoid DG Wrong Settled*/
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                        return transferRep;
                    }

                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    var result = _settledBLL.PayoutLC_DG(bettingDataLC_DG, out decimal tpAffterAmount);
                    if (result > 0 || result == -1/*has been Settled*/)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;
                        if (result == -1)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                            transferRep.member.balance = tpCustBalance.Balance - transferReq.member.amount;
                        }
                        else
                        {
                            transferRep.member.balance = tpAffterAmount - transferReq.member.amount;
                        }
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }
                else
                {
                    if (transferReq.data != transferReq.ticketId.ToString())/*avoid DG Wrong Settled*/
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                        return transferRep;
                    }

                    BettingDataLC_DG bettingDataLC_DG = new BettingDataLC_DG();
                    bettingDataLC_DG.Username = transferReq.member.username;
                    bettingDataLC_DG.Amount = Math.Abs(transferReq.member.amount);
                    bettingDataLC_DG.data = transferReq.data;
                    bettingDataLC_DG.TransId = transferReq.ticketId.ToString();

                    var result = _settledBLL.PayoutLC_DG(bettingDataLC_DG, out decimal tpAffterAmount);
                    if (result > 0 || result == -1/*has been Settled*/)
                    {
                        transferRep.codeId = (int)Error.OperationSuccessful;
                        transferRep.token = transferReq.token;

                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, transferReq.member.username, _gameType);
                        transferRep.member.balance = tpCustBalance.Balance;
                    }
                    else
                    {
                        transferRep.codeId = (int)Error.SystemError;
                    }
                }

                _log2File.Info($"{bodyText}\r\n{JsonConvert.SerializeObject(transferRep)}", logName);
            }
            catch (Exception ex)
            {
                transferRep.codeId = (int)Error.SystemError;
                _log2File.Error($"{bodyText}\r\n{JsonConvert.SerializeObject(transferRep)}\r\n{ex.Message}", logName, ex);
            }

            return transferRep;
        }

        [Route("[controller]/account/checkTransfer/{agentName}")]
        [Route("[controller]/{siteName}/account/checkTransfer/{agentName}")]
        [HttpPost]
        public async Task<CheckTransferRep> CheckTransferAsync(string agentName,string siteName = "")
        {
            string logName = $"{_tpName}_CheckTransfer";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string bodyText = await reader.ReadToEndAsync();
            var checkTransferReq = JsonConvert.DeserializeObject<CheckTransferReq>(bodyText);

            CheckTransferRep checkTransferRep = new CheckTransferRep();
            checkTransferRep.token = checkTransferReq.token;
            checkTransferRep.codeId = (int)Error.SystemError;
            try
            {
                if (!CheckToken(agentName,checkTransferReq.token,siteName))
                {
                    checkTransferRep.codeId = (int)Error.TokenVerificationFailed;
                    return checkTransferRep;
                }
                _log2File.Info($"{bodyText}\r\n{JsonConvert.SerializeObject(checkTransferRep)}", logName);
                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                checkTransferRep.codeId = (int)Error.SystemError;
                _log2File.Error($"{bodyText}\r\n{JsonConvert.SerializeObject(checkTransferRep)}\r\n{ex.Message}", logName, ex);
            }

            return checkTransferRep;
        }
        
        #region Private
        
        private bool CheckToken(string agentName, string reqToken, string siteName)
        {
            var config = GetSiteConfig(siteName);
            var token =  _tools.ToMD5($"{agentName}{config.APIkey}").ToLower();
            return reqToken == token;
        }

        private DGConfig GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return _defaultConfig;
            }
            return _siteNameConfigs.TryGetValue(siteName, out var result)
                ? result
                : _defaultConfig;
        }
        
        #endregion
    }
}