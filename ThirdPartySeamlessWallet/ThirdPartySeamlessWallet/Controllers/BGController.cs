using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using System;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.AspNetCore.SignalR;
using NuGet.Packaging.Signing;
using Google.Protobuf.WellKnownTypes;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class BGController : ControllerBase
{
    #region Fields

    private readonly Tp thisTp;

    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly IBGBLL _bGBLL;
    private readonly Tools _tools;
    private readonly ILogger<BGController> _logger;

    #endregion

    #region Constructors

    public BGController(IServiceProvider provider)
    {
        thisTp = Tp.BG;

        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _tools = provider.GetService<Tools>();
        _logger = provider.GetService<ILogger<BGController>>();
        _bGBLL = provider.GetService<IBGBLL>();
    }

    #endregion

    #region Action

    [Route("[controller]")]
    [HttpPost]
    public async Task<ResponseModel_BG> SeamlessWalletAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var Content = await reader.ReadToEndAsync();
        var requestContent = Content.TrimStart("json=".ToArray());
        var request = JsonConvert.DeserializeObject<SeamlessWalletRequest>(requestContent);

        switch (request.method)
        {
            //Ping
            case "open.operator.ping":
                return new ResponseModel_BG(request, ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds());

            //Balance
            case "open.operator.user.balance":
                if (!ConvertToAssignObject(request.@params, out BalanceParameters_BG balanceRequest, out string rawMessage))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: rawMessage);

                var signParameters = new SignParameters
                {
                    sign = balanceRequest.sign,
                    random = balanceRequest.random,
                    sn = balanceRequest.sn,
                    loginId = balanceRequest.loginId,
                };

                if (!CheckAppidAndSign(signParameters, out string message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, balanceRequest.sn);

                return await _bGBLL.Balance(request, balanceRequest);
            default:
                return new ResponseModel_BG(request, BGErrorCode.MethodNotFound, "Method not found.");
        }
    }

    #endregion

    #region Private Methods

    private class SignParameters
    {
        public string? sign { get; set; }
        public string? random { get; set; }
        public string? sn { get; set; }
        public string? loginId { get; set; }
        public string? amount { get; set; }
    }

    private bool ConvertToAssignObject<T>(object parameters, out T request, out string message)
    {
        message = string.Empty;
        request = default(T);
        try
        {
            request = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(parameters));
            return true;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return false;
        }
    }

    private bool CheckAppidAndSign(SignParameters parameters, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(parameters.sign))
        {
            message = "sign is invaild.";
            return false;
        }

        if (parameters.loginId == null)
        {
            message = "loginId not found.";
            return false;
        }

        // Get Cust.SiteId By request.loginId
        var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, parameters.loginId);
        if (tpCustInfo == null)
        {
            message = "TpCustInfo not found.";
            return false;
        }

        // Get Setting
        var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(_thirdPartyBLL.GetThirdPartyBySite((int)thisTp, tpCustInfo.SiteId).Setting);
        string secretKey = setting.ContainsKey("ApiSecretKey") && !string.IsNullOrEmpty(setting["ApiSecretKey"])
            ? setting["ApiSecretKey"]
            : throw new YeebetException(YeebetErrorCode.SystemError, "ApiSecretKey is invaild.");
        string sn = setting.ContainsKey("Sn") && !string.IsNullOrEmpty(setting["Sn"])
            ? setting["Sn"]
            : throw new YeebetException(YeebetErrorCode.SystemError, "Sn is invaild.");

        // Check sn
        if (sn != parameters.sn)
        {
            message = "request.sn is invaild.";
            return false;
        }

        //Check Sign
        var result = _tools.ToMD5(parameters.random + sn + parameters.loginId + parameters.amount + secretKey);
        if (result != parameters.sign)
        {
            message = $"sign is invaild. request.sign={parameters.sign}. correct sign={result}.";
            return false;
        }
        return true;
    }

    #endregion
}
