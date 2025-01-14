using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
[Route("[controller]")]
public class S1Controller : ControllerBase
{
    private readonly MaxGameBLL _maxGameBLL;
    private readonly Tools _tools;
    private readonly string _tpName = Tp.S1.ToString();
    private readonly Dictionary<int, MaxGameConfig> _siteConfigDic;
    private readonly PayloadLogService _payloadLogService;
    private const string _siteName = "S1"; // default siteName for single operator
    private const int _siteId = -1; // default siteId for single operator

    public S1Controller(IServiceProvider provider)
    {
        _maxGameBLL = new MaxGameBLL(provider);
        _tools = provider.GetRequiredService<Tools>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MaxGameConfig>(_tpName);
        _payloadLogService = provider.GetRequiredService<PayloadLogService>();
    }

    [HttpPost]
    [Route("GetPlayerInfo")]
    public async Task<dynamic> GetPlayerInfo()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(GetPlayerInfo), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("GetBalance")]
    public async Task<dynamic> GetBalance()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();
            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(GetBalance), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("PlaceBet")]
    public async Task<dynamic> PlaceBet()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(PlaceBet), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("EndGame")]
    public async Task<dynamic> EndGame()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(EndGame), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("PlaceBetAndSettle")]
    public async Task<dynamic> PlaceBetAndSettle()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(PlaceBetAndSettle), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("VoidGame")]
    public async Task<dynamic> VoidGame()
    {
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(VoidGame), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("Bonus")]
    public async Task<dynamic> Bonus()
    {
        // Not supported yet (please test if you want to use it)
        var timer = new Stopwatch();
        timer.Start();
        dynamic response = default;
        var content = string.Empty;
        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(Bonus), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    [HttpPost]
    [Route("FreeSpin")]
    public async Task<dynamic> FreeSpin()
    {
        // Not supported yet (please test if you want to use it)
        var timer = new Stopwatch();
        timer.Start();
        var content = "";
        dynamic response = default;

        try
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            content = await reader.ReadToEndAsync();

            var verifyHeader = CheckHeader(Request.Headers, content);
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
            _payloadLogService.LogDetailedPayload(nameof(FreeSpin), _siteName, content, response, timer.ElapsedMilliseconds);
        }
    }

    private bool CheckHeader(IHeaderDictionary header, string param)
    {
        string oSign = header["operator-sign"];
        if (string.IsNullOrEmpty(oSign))
        {
            return false;
        }

        var operatorKey = _siteConfigDic[_siteId].OperatorKey;

        var encrypt = _tools.ToMD5(param + operatorKey).ToLower();
        return oSign.ToLower() == encrypt;
    }
}