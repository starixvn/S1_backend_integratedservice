using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class MaxGameController : ControllerBase
    {
        private readonly ILogger<MaxGameController> _logger;
        private readonly MaxGameBLL _maxGameBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly string _tpName = Tp.MaxGame.ToString();
        private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;

        public MaxGameController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<MaxGameController>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _maxGameBLL = new MaxGameBLL(provider);
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.MaxGame;
            // 取共用，設定依機器分為(nova、aiking)
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/GetPlayerInfo")]
        public async Task<dynamic> GetPlayerInfo(string sitename)
        {
            string logName = $"{_thisTp}_GetPlayerInfo";
            
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();
                _logger.LogInformation("GetPlayerInfo 1 =" + content);
                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    var response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    WriteLogTest($"{sitename}/GetPlayerInfo", content, response, Request.Headers["operator-sign"]);
                    return response;
                }
                var result = _maxGameBLL.GetPlayerInfo(content);

                WriteLogTest($"{sitename}/GetPlayerInfo", content, result);
                _logger.LogInformation("GetPlayerInfo 2 =" + content);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("GetPlayerInfo 3 =" + ex.Message);
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/GetBalance")]
        public async Task<dynamic> GetBalance(string sitename)
        {
            string logName = $"{_thisTp}_GetBalance";

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();
                _logger.LogInformation(content);
                _logger.LogInformation("Get Balance 1 =" + content);
                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.GetBalance(content);

                WriteLogTest($"{sitename}/GetBalance", content, result);
                _logger.LogInformation("Get Balance 2=" + content);
                return result;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                _logger.LogInformation("Get Balance 3=" + ex.Message);
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBet")]
        public async Task<dynamic> PlaceBet(string sitename)
        {
            string logName = $"{_thisTp}_PlaceBet";
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = false;
                verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }
                var result = _maxGameBLL.PlaceBet(content);

                WriteLogTest($"{sitename}/PlaceBet", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/EndGame")]
        public async Task<dynamic> EndGame(string sitename)
        {
            string logName = $"{_thisTp}_EndGame";
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.EndGame(content);

                WriteLogTest($"{sitename}/EndGame", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBetAndSettle")]
        public async Task<dynamic> PlaceBetAndSettle(string sitename)
        {
            string logName = $"{_thisTp}_PlaceBetAndSettle";
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.PlaceBetAndSettle(content);

                WriteLogTest($"{sitename}/PlaceBetAndSettle", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/VoidGame")]
        public async Task<dynamic> VoidGame(string sitename)
        {
            string logName = $"{_thisTp}_VoidGame";
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.VoidGame(content);

                WriteLogTest($"{sitename}/VoidGame", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/Bonus")]
        public async Task<dynamic> Bonus(string sitename)
        {
            // 尚未支援 (如要使用要請測試)
            string logName = $"{_thisTp}_Bonus";

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.Bonus(content);

                WriteLogTest($"{sitename}/Bonus", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/FreeSpin")]
        public async Task<dynamic> FreeSpin(string sitename)
        {
            // 尚未支援 (如要使用要請測試)
            string logName = $"{_thisTp}_FreeSpin";

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                string content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    return new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                }

                var result = _maxGameBLL.FreeSpin(content);

                WriteLogTest($"{sitename}/FreeSpin", content, result);

                return result;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        private bool CheckHeader(IHeaderDictionary header, string param, string siteName, string logName)
        {
            string oSign = header["operator-sign"];
            if (string.IsNullOrEmpty(oSign))
            {
                _log2File.Error($"operator-sign Empty\r\noperator-sign={oSign}", logName);
                return false;
            }

            var serverInfo = _thirdPartyBLL.GetServerInfoBySiteName(siteName);
            if (serverInfo is not { SiteId: > 0 })
            {
                _log2File.Error($"SiteName Error\r\nsiteName={siteName}", logName);
                return false;
            }

            var siteId = serverInfo.SiteId;
            var operatorKey = _siteConfigDic[siteId].OperatorKey;

            var encrypt = _tools.ToMD5(param + operatorKey).ToLower();
            if (oSign.ToLower() == encrypt)
            {
                return true;
            }

            _log2File.Error($"CheckHeader Error\r\nHeader={oSign}\r\nencrypt={encrypt}", logName);
            return false;
        }

        private static void WriteLogTest(string action, string requestContent, dynamic result, string oSign = "")
        {
            SentrySdk.CaptureMessage($"{nameof(MaxGameController)}/{action} at {DateTime.Now}" +
                                     $"{Environment.NewLine}--requestContent: {requestContent}" +
                                     $"{Environment.NewLine}--header operator-sign: {oSign}" +
                                     $"{Environment.NewLine}---------------------" +
                                     $"{Environment.NewLine}--result: {Convert.ToString(result)}");
        }
    }
}