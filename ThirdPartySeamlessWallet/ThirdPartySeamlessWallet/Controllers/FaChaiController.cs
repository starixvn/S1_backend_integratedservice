using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class FaChaiController : ControllerBase
    {
        private readonly ILogger<FaChaiController> _logger;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly IFaChaiBLL _faChaiBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly List<Dictionary<string, string>> _configList;

        public Tp ThirdParty { get; set; }
        public int gameType { get; set; }

        #region Constructors
        public FaChaiController(IServiceProvider provider)
        {
            ThirdParty = Tp.FaChai;
            gameType = (int)GameType.FaChai_RNG;
            _logger = provider.GetRequiredService<ILogger<FaChaiController>>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _faChaiBLL = provider.GetRequiredService<IFaChaiBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _configList = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>((ThirdParty).ToString()).Select(x => x.Value).ToList();
        }
        #endregion

        public class GetBalanceData
        {
            public string MemberAccount { get; set; }
            public string Currency { get; set; }
            public string GameID { get; set; }
            public long Ts { get; set; }
        }
        public class GetDebitData
        {
            public string RecordID { get; set; }
            public string BankID { get; set; }
            public string MemberAccount { get; set; }
            public string Currency { get; set; }
            public int GameID { get; set; }
            public int GameType { get; set; }
            public bool isBuyFeature { get; set; }
            public decimal Bet { get; set; }
            public decimal Win { get; set; }
            public decimal JPBet { get; set; }
            public decimal JPPrize { get; set; }
            public decimal NetWin { get; set; }
            public decimal RequireAmt { get; set; }
            public DateTime GameDate { get; set; }
            public DateTime CreateDate { get; set; }
            public long Ts { get; set; }
        }

        [Route("Fachai/getbalance")]
        [HttpPost]
        public FCResponse GetBalance([FromForm] FCRequest req)
        {
            var siteConfig = _configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = _tools.AesDecrypt(req.Params, siteConfig["Key"], false);

            if (para == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Verification_failed
                };
            }
            _logger.LogInformation(para);

            var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceData>(para);
            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, getBalanceReq.MemberAccount, gameType);

            return new FCResponse()
            {
                Result = (int)FCErrorMessage.Success,
                MainPoints = decimal.ToSingle(tpCustBalance.Balance),
            };
        }

        [Route("Fachai/betinfo")]
        [HttpPost]

        public FCResponse Bet([FromForm] FCRequest req)
        {
            var siteConfig = _configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = _tools.AesDecrypt(req.Params, siteConfig["Key"], false);

            if (para == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Verification_failed
                };
            }
            _logger.LogInformation(para);


            var bettingData = JsonConvert.DeserializeObject<BettingDataRNG_FaChai>(para);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.FaChai, bettingData.MemberAccount);

            if (tpCheck == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Account_does_not_exist
                };
            }
            else if (tpCheck.Locked)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Account_locked
                };
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, bettingData.MemberAccount, gameType);

            if (tpCustBalance.Balance < bettingData.Bet)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Your_points_balance_not_enough_to_withdraw
                };
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, bettingData.MemberAccount);

            var result = _faChaiBLL.Bet(tpCustInfo, bettingData, out decimal afterAmount);

            if (result.Result == (int)FCErrorMessage.Success)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Success,
                    MainPoints = decimal.ToSingle(afterAmount)
                };
            }

            return result;
        }

        [Route("Fachai/cancelbetinfo")]
        [HttpPost]
        public FCResponse Cancel([FromForm] FCRequest req)
        {
            var siteConfig = _configList.Find(x => x["AgentCode"] == req.AgentCode);
            var para = _tools.AesDecrypt(req.Params, siteConfig["Key"], false);

            if (para == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Verification_failed
                };
            }
            _logger.LogInformation(para);

            var cancelData = JsonConvert.DeserializeObject<CnacelData_FaChai>(para);

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, cancelData.MemberAccount);

            if (tpCustInfo == null)
            {
                return new FCResponse()
                {
                    Result = (int)FCErrorMessage.Account_does_not_exist
                };
            }

            var result = _faChaiBLL.Cancel(tpCustInfo, cancelData);

            return result;
        }
    }
}
