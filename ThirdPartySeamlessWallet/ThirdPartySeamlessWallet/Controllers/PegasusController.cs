using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class PegasusController : ControllerBase
{
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly Log2File _log2File;
    private readonly IPegasusBLL _pegasusBll;
    private readonly string _tpGameName;

    public PegasusController(IServiceProvider provider)
    {
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
        _log2File = provider.GetRequiredService<Log2File>();
        _pegasusBll = provider.GetRequiredService<IPegasusBLL>();
        _tpGameName = Tp.Pegasus.ToString();
    }
    
    [HttpPost]
    [Route("[controller]/pgs/players/wallet")]
    public GetBalanceResp GetBalance(GetBalanceReq req)
    {
        var logName = $"{_tpGameName}_GetBalance";
        var checkPlayerResp = new GetBalanceResp {Data = new PGSCustBalance()};

        try
        {
            var tokenInfo = GetTokenInfo(req.SessionToken);
            
            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)Tp.Pegasus, tokenInfo.TpUserName, (int)GameType.Pegasus_RNG);
            checkPlayerResp.Code = (int)PegasusResultCode.Success;
            checkPlayerResp.Data.Balance = tpCustBalance.Balance;
            return checkPlayerResp;
        }
        catch (Exception e)
        {
            checkPlayerResp.Code = (int)PegasusResultCode.Exception;
            _log2File.Error($"SessionToken={req.SessionToken}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName, e);
            return checkPlayerResp;
        }
    }
    
    [HttpPost]
    [Route("[controller]/pgs/bet/placeBet")]
    public async Task<Response_Pegasus<ResultPegasus>> PlaceBet()
    {
        var logName = $"{_tpGameName}_PlaceBet";
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        var response = new Response_Pegasus<ResultPegasus>
        {
            Code = (int)PegasusResultCode.Exception
        };

        try
        {
            var getReq = JsonConvert.DeserializeObject<BettingRequest_Pegasus>(content);
            
            var tokenInfo = GetTokenInfo(getReq.SessionToken);
            if (tokenInfo.TpUserName != getReq.PlayerUniqueID)
            {
                response.Code = (int)PegasusResultCode.PlayerDoesNotExist;
                response.Message = "Player is different between PlayerUniqueID and SessionToken";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            
            var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)Tp.Pegasus, tokenInfo.TpUserName);
            if (tpCheck == null)
            {
                response.Code = (int)PegasusResultCode.PlayerDoesNotExist;
                response.Message = "Player Does Not Exist";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            else if (tpCheck.Locked)
            {
                response.Code = (int)PegasusResultCode.PlayerIsInactive;
                response.Message = "Player Is Inactive";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }

            var custBalance = _tpCustInfoBll.GetBalance((int)Tp.Pegasus, tokenInfo.TpUserName, (int)GameType.Pegasus_RNG);
            var checkBalance = custBalance.Balance - getReq.BetAmount;
            if (checkBalance < 0)
            {
                response.Code = (int)PegasusResultCode.NotEnoughBalance;
                response.Message = "Not enough balance";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }

            var result = _pegasusBll.Bet_Pegasus(getReq, tokenInfo.TpUserName, Tp.Pegasus, GameType.Pegasus_RNG, out var outData);

            if (result > 0)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "Success";
                response.Data = outData;
                _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            else if( result == -1)
            {
                response.Code = (int)PegasusResultCode.Duplicate;
                response.Message = "TransactionNo Exists";
                _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            
        }
        catch (Exception ex)
        {
            response.Message = "Unknown Error";
            _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
            return response;
        }
        return response;
    }
    
    [HttpPost]
    [Route("[controller]/pgs/bet/settle")]
    public async Task<Response_Pegasus<ResultPegasus>> Settle()
    {
        var logName = $"{_tpGameName}Settle";
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        var response = new Response_Pegasus<ResultPegasus>
        {
            Code = (int)PegasusResultCode.Exception,
            Message = "Unknown Error"
        };

        try
        {
            var getReq = JsonConvert.DeserializeObject<SettleRequest_Pegasus>(content);
            
            var tokenInfo = GetTokenInfo(getReq.SessionToken);
            if (tokenInfo.TpUserName != getReq.PlayerUniqueID)
            {
                response.Code = (int)PegasusResultCode.PlayerDoesNotExist;
                response.Message = "Player is different between PlayerUniqueID and SessionToken";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            
            var result = _pegasusBll.Settle_Pegasus(getReq, tokenInfo.TpUserName, Tp.Pegasus, GameType.Pegasus_RNG, out var outData);

            if (result > 0)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "Success";
                response.Data = outData;
                _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            if (result == -3)
            {
                response.Code = (int)PegasusResultCode.InvalidArgument;
                response.Message = "Already accepted";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
            }
            if (result == -2)
            {
                response.Code = (int)PegasusResultCode.TransactionIdNotFound;
                response.Message = "TransactionNo Not Found";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
            }
        }
        catch (Exception ex)
        {
            _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
            return response;
        }
        return response;
    }
    
     [HttpPost]
    [Route("[controller]/pgs/bet/cancel")]
    public async Task<Response_Pegasus<ResultPegasus>> Cancel()
    {
        var logName = $"{_tpGameName}cancel";
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        var response = new Response_Pegasus<ResultPegasus>
        {
            Code = (int)PegasusResultCode.Exception,
            Message = "Unknown Error"
        };

        try
        {
            var getReq = JsonConvert.DeserializeObject<CancelRequest_Pegasus>(content);
            
            var tokenInfo = GetTokenInfo(getReq.SessionToken);
            if (tokenInfo.TpUserName != getReq.PlayerUniqueID)
            {
                response.Code = (int)PegasusResultCode.PlayerDoesNotExist;
                response.Message = "Player is different between PlayerUniqueID and SessionToken";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            
            var result = _pegasusBll.Cancel_Pegasus(getReq, tokenInfo.TpUserName, Tp.Pegasus, GameType.Pegasus_RNG, out var outData);

            if (result > 0)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "Success";
                response.Data = outData;
                _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            if (result == -2)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "TransactionNo Not Found";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
            }
            if (result == -3)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "Already accepted";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
            }
        }
        catch (Exception ex)
        {
            _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
            return response;
        }
        return response;
    }
    
    [HttpPost]
    [Route("[controller]/pgs/transaction/event-bonus")]
    public async Task<Response_Pegasus<BonusResultPegasus>> EventBonus()
    {
        var logName = $"{_tpGameName}EventBonus";
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();

        var response = new Response_Pegasus<BonusResultPegasus>
        {
            Code = (int)PegasusResultCode.Exception,
            Message = "Unknown Error"
        };

        try
        {
            var getReq = JsonConvert.DeserializeObject<BonusRequest_Pegasus>(content);
            
            var result = _pegasusBll.Bonus_Pegasus(getReq, getReq.PlayerUniqueID, Tp.Pegasus, GameType.Pegasus_RNG, out var outData);

            if (result > 0)
            {
                response.Code = (int)PegasusResultCode.Success;
                response.Message = "Success";
                response.Data = outData;
                _log2File.Info($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
                return response;
            }
            if (result == -3)
            {
                response.Code = (int)PegasusResultCode.Duplicate;
                response.Message = "Duplicate TransactionNo Exists";
                _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName);
            }
        }
        catch (Exception ex)
        {
            _log2File.Error($"{content}\r\n{JsonConvert.SerializeObject(response)}", logName, ex);
            return response;
        }
        return response;
    }
    
    private static PegasusTokenInfo GetTokenInfo(string token)
    {
        var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
        var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
        return new PegasusTokenInfo
        {
            Token = sessionKeyArr[0],
            TpUserName = sessionKeyArr[1]
        };
    }

}
