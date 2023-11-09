using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.HL
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly HLConfig _siteHLConfig;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(TpEnum.HaoLong.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult();
            var createMemberReq = new CreateMemberReq
            {
                ApiUser = _siteHLConfig.ApiUser,
                ApiPass = _siteHLConfig.ApiPass,
                AgentUser = _siteHLConfig.AgentUser,
                AgentPass = _siteHLConfig.AgentPass,
                PlayerID = tpCustInfo.TpUserName,
                PlayerPass = tpCustInfo.TpPwd,
                PlayerName = tpCustInfo.TpUserName
            };

            var postData = $"apiUser={createMemberReq.ApiUser}&apiPass={createMemberReq.ApiPass}&" +
                              $"user={createMemberReq.AgentUser}&pass={createMemberReq.AgentPass}&" +
                              $"loginID={createMemberReq.PlayerID}&loginPass={createMemberReq.PlayerPass}&fullName={createMemberReq.PlayerName}";
            
            var result = HttpPost($"{_siteHLConfig.ApiUrl}createPlayer.aspx?",postData ,"application/x-www-form-urlencoded");
            if (IsXML(result))
            {
                HLResponse hlResponse;
                try
                {
                    hlResponse = XMLDeserialize<HLResponse>(result);
                }
                catch (Exception)
                {
                    hlResponse = XMLDeserialize<HLResponse>(result);
                }

                if (hlResponse.ErrorCode == 0)
                {
                    oApiResult.RawMessage = hlResponse.Desc.ToString();
                    oApiResult.RawErrorCode = hlResponse.ErrorCode.ToString();
                    oApiResult.ErrorCode = ErrorCode.Fail;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult();
            var getLoginInformationReq = new GetLoginInformationReq
            {
                ApiUser = _siteHLConfig.ApiUser,
                ApiPass = _siteHLConfig.ApiPass,
                PlayerID = tpCustInfo.TpUserName,
                PlayerPass = tpCustInfo.TpPwd
            };
            
            //這邊的User&Pass非BO的User和Pass，而是玩家的User和Pass
            var postData = $"apiUser={getLoginInformationReq.ApiUser}&apiPass={getLoginInformationReq.ApiPass}&" +
                           $"user={getLoginInformationReq.PlayerID}&pass={getLoginInformationReq.PlayerPass}";
            
            var result = HttpPost($"{_siteHLConfig.ApiUrl}betLogin.aspx?",postData ,"application/x-www-form-urlencoded");
            if (IsXML(result))
            {
                HLResponse hlResponse;
                try
                {
                    hlResponse = XMLDeserialize<BetLoginResponse>(result);
                }
                catch (Exception)
                {
                    hlResponse = XMLDeserialize<HLResponse>(result);
                }

                if (hlResponse.ErrorCode == 0)
                {
                    var loginReq = new LoginReq
                    {
                        PlayerID = tpCustInfo.TpUserName,
                        SessionID = ((BetLoginResponse)hlResponse).SessionID,
                        TokenCode = ((BetLoginResponse)hlResponse).TokenCode
                    };

                    if (appType == AppType.Desktop)
                    {
                        postData = $"user={loginReq.PlayerID}&sessionID={loginReq.SessionID}&tokenCode={loginReq.TokenCode}";
                        oApiResult.Message = $"{_siteHLConfig.ApiUrl}bet.aspx?{postData}";
                    }
                    else
                    {
                        postData = $"user={loginReq.PlayerID}&sessionID={loginReq.SessionID}&tokenCode={loginReq.TokenCode}"; 
                        oApiResult.Message = $"{_siteHLConfig.ApiUrl}wap/main.aspx?{postData}";
                    }
                
                    oApiResult.ErrorCode = ErrorCode.Success;
                    
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
            }
            
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            var oApiResult = new ApiResult<TransferResult>();
            return oApiResult;
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            var oApiResult = new ApiResult<TransferResult>();
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return null;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            return null;
        }
    }
}