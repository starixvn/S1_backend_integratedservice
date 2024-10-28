using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
[Route("[controller]")]
public class BGController : ControllerBase
{
    #region Fields

    private readonly IBGBLL _bGBLL;

    #endregion

    #region Constructors

    public BGController(IServiceProvider provider)
        => _bGBLL = provider.GetService<IBGBLL>();

    #endregion

    #region Action

    [HttpPost]
    public async Task<ResponseModel_BG> SeamlessWalletAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<SeamlessWalletRequest>(content);
        LogContext.PushProperty("Method", request.method);
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

                if (!CheckAppidAndSign(new SignParameters_BG
                {
                    sign = userBalanceParameters.sign,
                    random = userBalanceParameters.random,
                    sn = userBalanceParameters.sn,
                    loginId = userBalanceParameters.loginId,
                }, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, userBalanceParameters.sn);

                return _bGBLL.UserBalance(request, userBalanceParameters);

            //Bet
            case "open.operator.order.transfer":
                if (!ConvertToAssignObject(request.@params, out OrderTransferParameters_BG orderTransferParameters, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Convert parameter fail.", reason: message);

                if (!CheckAppidAndSign(new SignParameters_BG
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

                if (!CheckAppidAndSign(new SignParameters_BG
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

                if (!CheckAppidAndSign(new SignParameters_BG
                {
                    sign = orderStatusParameters.sign,
                    random = orderStatusParameters.random,
                    sn = orderStatusParameters.sn,
                    loginId = orderStatusParameters.orderMap.First().loginId
                }, out message, false))
                    return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, orderStatusParameters.sn);

                return _bGBLL.OrderStatus(request, orderStatusParameters);
            default:
                return new ResponseModel_BG(request, BGErrorCode.MethodNotFound, "Method not found.");
        }
    }

    #endregion

    #region Private Methods

    private static bool ConvertToAssignObject<T>(object parameters, out T request, out string message)
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

    private bool CheckAppidAndSign(SignParameters_BG parameters, out string message, bool isSignNeedLoginId = true)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(parameters.sign))
        {
            message = "CheckRequest Fail: sign is invaild.";
            return false;
        }

        //Check Sign
        var sinResult = _bGBLL.GetSign(parameters, isSignNeedLoginId);
        if (sinResult != parameters.sign)
        {
            message = $"sign is invaild. request.sign={parameters.sign}. correct sign={sinResult}.";
            return false;
        }
        return true;
    }

    #endregion
}
