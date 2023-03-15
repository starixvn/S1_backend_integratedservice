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
        var message = string.Empty;

        switch (request.method)
        {
            //Ping
            case "open.operator.ping":
                return new ResponseModel_BG(request, ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds());

            //Balance
            case "open.operator.user.balance":
                if (!ConvertToAssignObject(request.@params, out UserBalanceParameters_BG userBalanceParameters, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: message);

                if (!CheckAppidAndSign(new SignParameters
                {
                    sign = userBalanceParameters.sign,
                    random = userBalanceParameters.random,
                    sn = userBalanceParameters.sn,
                    loginId = userBalanceParameters.loginId,
                }, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, userBalanceParameters.sn);

                return await _bGBLL.UserBalance(request, userBalanceParameters);

            //Bet
            case "open.operator.order.transfer":
                if (!ConvertToAssignObject(request.@params, out OrderTransferParameters_BG orderTransferParameters, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: message);

                if (!CheckAppidAndSign(new SignParameters
                {
                    sign = orderTransferParameters.sign,
                    random = orderTransferParameters.random,
                    sn = orderTransferParameters.sn,
                    loginId = orderTransferParameters.loginId,
                    amount = orderTransferParameters.amount,
                }, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, orderTransferParameters.sn);

                return await _bGBLL.OrderTransfer(request, orderTransferParameters);

            //Payout (ReSettle)
            case "open.operator.calc.transfer":
                if (!ConvertToAssignObject(request.@params, out CalcTransferParameters_BG calcTransferParameters, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: message);

                if (!CheckAppidAndSign(new SignParameters
                {
                    sign = calcTransferParameters.sign,
                    random = calcTransferParameters.random,
                    sn = calcTransferParameters.sn,
                    loginId = calcTransferParameters.loginId,
                    amount = calcTransferParameters.amount
                }, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, calcTransferParameters.sn);

                return await _bGBLL.CalcTransfer(request, calcTransferParameters);

            //Bettrans Check (BG重新結算機制，會先跟確認注單於我方的目前狀況)
            case "open.operator.order.status":
                if (!ConvertToAssignObject(request.@params, out OrderStatusParameters_BG orderStatusParameters, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: message);

                if (!CheckAppidAndSign(new SignParameters
                {
                    sign = orderStatusParameters.sign,
                    random = orderStatusParameters.random,
                    sn = orderStatusParameters.sn,
                    loginId = orderStatusParameters.orderMap.First().loginId
                }, out message, false))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, orderStatusParameters.sn);

                return await _bGBLL.OrderStatus(request, orderStatusParameters);
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

    private bool CheckAppidAndSign(SignParameters parameters, out string message, bool isSignNeedLoginId = true)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(parameters.sign))
        {
            message = "CheckRequest Fail: sign is invaild.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.sn))
        {
            message = "CheckRequest Fail: sn is invaild.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.loginId))
        {
            message = "CheckRequest Fail: loginId is invaild.";
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
        var result = _tools.ToMD5(parameters.random + sn + (isSignNeedLoginId ? parameters.loginId : "") + parameters.amount + secretKey);
        if (result != parameters.sign)
        {
            message = $"sign is invaild. request.sign={parameters.sign}. correct sign={result}.";
            return false;
        }
        return true;
    }

    #endregion
}
