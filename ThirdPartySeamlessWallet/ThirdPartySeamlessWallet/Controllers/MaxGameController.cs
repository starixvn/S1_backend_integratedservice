using System.Diagnostics;
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
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_GetPlayerInfo";
            var content = "";
            dynamic response = default;

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    return response;
                }

                response = _maxGameBLL.GetPlayerInfo(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(GetPlayerInfo), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/GetBalance")]
        public async Task<dynamic> GetBalance(string sitename)
        {
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_GetBalance";
            var content = "";
            dynamic response = default;
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();
                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);

                    return response;
                }

                response = _maxGameBLL.GetBalance(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(GetBalance), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBet")]
        public async Task<dynamic> PlaceBet(string sitename)
        {
            var timer = new Stopwatch();
            timer.Start();
            var content = "";
            var logName = $"{_thisTp}_PlaceBet";
            dynamic response = default;

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = false;
                verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    return response;
                }

                response = _maxGameBLL.PlaceBet(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(PlaceBet), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/EndGame")]
        public async Task<dynamic> EndGame(string sitename)
        {
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_EndGame";
            var content = "";
            dynamic response = default;
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);

                    return response;
                }
                response = response = _maxGameBLL.EndGame(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(EndGame), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/PlaceBetAndSettle")]
        public async Task<dynamic> PlaceBetAndSettle(string sitename)
        {
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_PlaceBetAndSettle";
            var content = "";
            dynamic response = default;
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);

                    return response;
                }

                response = _maxGameBLL.PlaceBetAndSettle(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(PlaceBetAndSettle), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/VoidGame")]
        public async Task<dynamic> VoidGame(string sitename)
        {
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_VoidGame";
            var content = "";
            dynamic response = default;
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    return response;
                }

                response = _maxGameBLL.VoidGame(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(VoidGame), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/Bonus")]
        public async Task<dynamic> Bonus(string sitename)
        {
            // 尚未支援 (如要使用要請測試)
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_Bonus";
            dynamic response = default;
            var content = string.Empty;
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    return response;
                }

                response = _maxGameBLL.Bonus(content);

                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(Bonus), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        [HttpPost]
        [Route("[controller]/{sitename}/FreeSpin")]
        public async Task<dynamic> FreeSpin(string sitename)
        {
            // 尚未支援 (如要使用要請測試)
            var timer = new Stopwatch();
            timer.Start();
            var logName = $"{_thisTp}_FreeSpin";
            var content = "";
            dynamic response = default;

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                content = await reader.ReadToEndAsync();

                var verifyHeader = CheckHeader(Request.Headers, content, sitename, logName);
                if (!verifyHeader)
                {
                    var getReq = JsonConvert.DeserializeObject<CommonReq>(content);
                    response = new BaseResp(ErrorCodeOfMaxGame.OpSideSignInvalid, getReq.Seq);
                    return response;
                }

                response = _maxGameBLL.FreeSpin(content);
                return response;
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                response = new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError);
                return response;
            }
            finally
            {
                timer.Stop();
                _payloadLogService.LogDetailedPayload(nameof(FreeSpin), sitename, content, response, timer.ElapsedMilliseconds);
            }
        }

        private bool CheckHeader(IHeaderDictionary header, string param, string siteName, string logName)
        {

            _logger.LogInformation("operator-sign: "+header["operator-sign"]);
            string oSign = header["operator-sign"];
            if (string.IsNullOrEmpty(oSign))
            {
                return false;
            }

            var serverInfo = _thirdPartyBLL.GetServerInfoBySiteName(siteName);
           
            if (serverInfo is not { SiteId: > 0 })
            {
                return false;
            }

            var siteId = serverInfo.SiteId;
            var operatorKey = _siteConfigDic[siteId].OperatorKey;
            _logger.LogInformation("operatorKey: " + operatorKey);
            var encrypt = _tools.ToMD5(param + operatorKey).ToLower();
            _logger.LogInformation("encrypt: " + encrypt);
            if (oSign.ToLower() == encrypt)
            {
                return true;
            }

            return false;
        }
    }
}