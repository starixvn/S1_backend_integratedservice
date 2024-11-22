using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
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

        private readonly PayloadLogService _payloadLogService;

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
            _payloadLogService = provider.GetRequiredService<PayloadLogService>();
        }

        [HttpPost]
        [Route("[controller]/{sitename}/GetPlayerInfo")]
        public async Task<dynamic> GetPlayerInfo(string sitename)
        {
            string logName = $"{_thisTp}_GetPlayerInfo";
            string content = "";
            var logMeta = new PayloadLogMeta()
            {
                Action = nameof(GetPlayerInfo)
            };

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);
                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    var response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                    return response;
                }

                var result = _maxGameBLL.GetPlayerInfo(content);

                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, result, logMeta);

                return result;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError), logMeta);
            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/GetBalance")]
        public async Task<dynamic> GetBalance(string sitename)
        {
            string logName = $"{_thisTp}_GetBalance";
            string content = "";
            dynamic response;
            var logMeta = new PayloadLogMeta()
            {
                Action = nameof(GetBalance),
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);
                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }

                var result = _maxGameBLL.GetBalance(content);

                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, result, logMeta);
                return result;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
            return response;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBet")]
        public async Task<dynamic> PlaceBet(string sitename)
        {
            string content = "";
            string logName = $"{_thisTp}_PlaceBet";
            dynamic response;
            var logMeta = new PayloadLogMeta()
            {
                Action = nameof(PlaceBet),
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);

                var verifyHeader = false;
                verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }
                response = _maxGameBLL.PlaceBet(content);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);

            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
            return response;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/EndGame")]
        public async Task<dynamic> EndGame(string sitename)
        {
            string logName = $"{_thisTp}_EndGame";
            string content = "";
            dynamic response;
            var logMeta = new PayloadLogMeta
            {
                Action = nameof(EndGame)
            };

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);

                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }
                response = response = _maxGameBLL.EndGame(content);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
            return response;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBetAndSettle")]
        public async Task<dynamic> PlaceBetAndSettle(string sitename)
        {
            string logName = $"{_thisTp}_PlaceBetAndSettle";
            string content = "";
            dynamic response;
            var logMeta = new PayloadLogMeta
            {
                Action = nameof(PlaceBetAndSettle)
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);

                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }

                response = _maxGameBLL.PlaceBetAndSettle(content);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
            return response;
        }

        [HttpPost]
        [Route("[controller]/{sitename}/VoidGame")]
        public async Task<dynamic> VoidGame(string sitename)
        {
            string logName = $"{_thisTp}_VoidGame";
            string content = "";
            dynamic response;
            var logMeta = new PayloadLogMeta
            {
                Action = nameof(VoidGame)
            };
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }

                response = _maxGameBLL.VoidGame(content);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }

            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
            return response;
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
                SentrySdk.CaptureException(ex);
            }

            return new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
        }

        [HttpPost]
        [Route("[controller]/{sitename}/FreeSpin")]
        public async Task<dynamic> FreeSpin(string sitename)
        {
            // 尚未支援 (如要使用要請測試)
            string logName = $"{_thisTp}_FreeSpin";
            string content = "";
            dynamic response;
            var logMeta = new PayloadLogMeta
            {
                Action = nameof(FreeSpin)
            };

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                _payloadLogService.LogDetailedPayload(content, logMeta);

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);
                    return response;
                }

                response = _maxGameBLL.FreeSpin(content);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
            response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
            _payloadLogService.UpdateDetailedPayLoadWithResponse(content, response, logMeta);

            return response;
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