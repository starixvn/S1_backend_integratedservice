using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class PegasusController : ControllerBase
{
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly Log2File _log2File;
    private readonly string _tpGameName;

    public PegasusController(IServiceProvider provider)
    {
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
        _log2File = provider.GetRequiredService<Log2File>();
        _tpGameName = Tp.Pegasus.ToString();
    }
    
    public class GetBalanceReq
    {
        public string SessionToken { get; set; }
    }
    
    public class GetBalanceResp
    {
        public int Code { get; set; }
        public PGSCustBalance Data { get; set; }
    }

    public class PGSCustBalance
    {
        public decimal Balance { get; set; }
    }

    private class TokenInfo
    {
        public string Token { get; set; }
        public string TpUserName { get; set; }
        public string CustId { get; set; }
        public string GameId { get; set; }
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
            checkPlayerResp.Code = (int)IMResultCode.Success;
            checkPlayerResp.Data.Balance = tpCustBalance.Balance;
            return checkPlayerResp;
        }
        catch (Exception e)
        {
            checkPlayerResp.Code = (int)IMResultCode.Exception;
            _log2File.Error($"SessionToken={req.SessionToken}\r\n{JsonConvert.SerializeObject(checkPlayerResp)}", logName, e);
            return checkPlayerResp;
        }
    }
    
    private TokenInfo GetTokenInfo(string token)
    {
        var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
        var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
        return new TokenInfo
        {
            Token = sessionKeyArr[0],
            CustId = sessionKeyArr[1],
            TpUserName = sessionKeyArr[2],
            GameId = sessionKeyArr[3]
        };
    }

}
