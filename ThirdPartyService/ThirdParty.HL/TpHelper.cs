using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.HL
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly HLConfig _siteHLConfig;
        private readonly SiteLanguageBLL _siteLanguageBLL;
        
        public TpHelper(IServiceProvider provider)
        {
            _siteLanguageBLL = provider.GetRequiredService<SiteLanguageBLL>();
            _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(TpEnum.HaoLong.ToString());
        }
        
        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult();
            if (CheckHaoLongBusyTime(1))
            {
                var langId = inputs.ContainsKey("langId") && !string.IsNullOrEmpty(inputs["langId"]) ? inputs["langId"] : "en";
                oApiResult.ErrorCode = ErrorCode.ServerIsBusy;
                oApiResult.Message = _siteLanguageBLL.GetLanguageValue(tpCustInfo.SiteId,"HaoLongCreateMemberBusyTime",langId);
                return oApiResult;
            }
            
            
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

                if (hlResponse.ErrorCode is 0 or 4)
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
                else
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
                BetLoginResponse betLoginResponse;
                try
                {
                    betLoginResponse = XMLDeserialize<BetLoginResponse>(result);
                }
                catch (Exception)
                {
                    betLoginResponse = XMLDeserialize<BetLoginResponse>(result);
                }

                if (betLoginResponse.ErrorCode == 0)
                {
                    var loginReq = new LoginReq
                    {
                        PlayerID = tpCustInfo.TpUserName,
                        SessionID = betLoginResponse.SessionID,
                        TokenCode = betLoginResponse.TokenCode
                    };
                    
                    postData = $"user={loginReq.PlayerID}&sessionID={loginReq.SessionID}&tokenCode={loginReq.TokenCode}";
                    
                    oApiResult.Message = appType == AppType.Desktop
                        ? $"{_siteHLConfig.ApiUrl}bet.aspx?{postData}"
                        : $"{_siteHLConfig.MobileGameUrl}wap/main.aspx?{postData}";
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
            var oApiResult = new ApiResult<UserBalance>();
            if (CheckHaoLongBusyTime(2))
            {
                var langId = inputs.ContainsKey("langId") && !string.IsNullOrEmpty(inputs["langId"]) ? inputs["langId"] : "en";
                oApiResult.ErrorCode = ErrorCode.ServerIsBusy;
                oApiResult.Message = _siteLanguageBLL.GetLanguageValue(tpCustInfo.SiteId,"HaoLongTransferBusyTime",langId);
                return oApiResult;
            }
            
            var getProfileReq = new GetProfileReq
            {
                ApiUser = _siteHLConfig.ApiUser,
                ApiPass = _siteHLConfig.ApiPass,
                AgentUser = _siteHLConfig.AgentUser,
                AgentPass = _siteHLConfig.AgentPass,
                PlayerID = tpCustInfo.TpUserName
            };
            
            var postData = $"apiUser={getProfileReq.ApiUser}&apiPass={getProfileReq.ApiPass}&" +
                           $"user={getProfileReq.AgentUser}&pass={getProfileReq.AgentPass}&" +
                           $"loginID={getProfileReq.PlayerID}";
            
            var result = HttpPost($"{_siteHLConfig.ApiUrl}getProfile.aspx?",postData ,"application/x-www-form-urlencoded");
            if (IsXML(result))
            {
                GetProfileResponse betLoginResponse;
                try
                {
                    betLoginResponse = XMLDeserialize<GetProfileResponse>(result);
                }
                catch (Exception)
                {
                    betLoginResponse = XMLDeserialize<GetProfileResponse>(result);
                }

                if (betLoginResponse.ErrorCode == 0)
                {
                    var parseBalance = Convert.ToDecimal(betLoginResponse.Balance);
                    var parseOutstanding = Convert.ToDecimal(betLoginResponse.Outstanding);
                    
                    oApiResult.Data = new UserBalance
                    {
                        balance = parseBalance,
                        outstanding = parseOutstanding 
                    };
                    oApiResult.ErrorCode = ErrorCode.Success;
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
            }
            
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            var oApiResult = new ApiResult<TransferResult>();
            if (CheckHaoLongBusyTime(2))
            {
                var langId = inputs.ContainsKey("langId") && !string.IsNullOrEmpty(inputs["langId"]) ? inputs["langId"] : "en";
                oApiResult.ErrorCode = ErrorCode.ServerIsBusy;
                oApiResult.Message = _siteLanguageBLL.GetLanguageValue(tpCustInfo.SiteId,"HaoLongTransferBusyTime",langId);
                return oApiResult;
            }
            
            var apiUrl = _siteHLConfig.ApiUrl;
            var postData = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                                $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&" +
                                $"loginID={tpCustInfo.TpUserName}&amount={Amount}&remarks={TransID}";
            apiUrl = direction switch
            {
                Direction.ToTp => apiUrl + "deposit.aspx?",
                Direction.ToMain => apiUrl + "withdraw.aspx?",
                _ => apiUrl
            };
            var result = HttpPost(apiUrl,postData ,"application/x-www-form-urlencoded");

            if (IsXML(result))
            {
                HLResponse profileResponse;
                try
                {
                    profileResponse = XMLDeserialize<HLResponse>(result);
                }
                catch (Exception)
                {
                    profileResponse = XMLDeserialize<HLResponse>(result);
                }

                return profileResponse.ErrorCode switch
                {
                    5 => new ApiResult<TransferResult>()
                    {
                        ErrorCode = ErrorCode.TransferFail,
                        RawErrorCode = profileResponse.ErrorCode.ToString(),
                        RawMessage = "Invalid Amount",
                        Data = new TransferResult() { Status = TransferStatus.Fail, }
                    },
                    99 => new ApiResult<TransferResult>()
                    {
                        ErrorCode = ErrorCode.TransferFail,
                        RawErrorCode = profileResponse.ErrorCode.ToString(),
                        RawMessage =
                            "Grand Dragon Transfer is currently undergoing maintenance, temporarily unavailable to transfer. Please try again after.",
                        Data = new TransferResult() { Status = TransferStatus.Fail, }
                    },
                    _ => new ApiResult<TransferResult>()
                    {
                        ErrorCode = ErrorCode.Success,
                        Data = new TransferResult() { Status = TransferStatus.Success }
                    }
                };
            }
            
            return new ApiResult<TransferResult>()
            {
                ErrorCode = ErrorCode.TransferPending,
                Data = new TransferResult()
                {
                    Status = TransferStatus.Pending
                }
            };
            
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            var oApiResult = new ApiResult<TransferResult>();
            if (CheckHaoLongBusyTime(2))
            {
                oApiResult.ErrorCode = ErrorCode.ServerIsBusy;
                oApiResult.Message = _siteLanguageBLL.GetLanguageValue(tpCustInfo.SiteId,"HaoLongTransferBusyTime","en"); //先寫死en,要不然都會依照幣別固定語言
                return oApiResult;
            }
            
            var postData = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                                $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&" +
                                $"loginID={tpCustInfo.TpUserName}&remarks={TransID}";
            
            var result = HttpPost($"{_siteHLConfig.ApiUrl}txnList.aspx",postData ,"application/x-www-form-urlencoded");

            if (IsXML(result))
            {
                TransferCheckResponse transferCheckResponse;
                try
                {
                    transferCheckResponse = XMLDeserialize<TransferCheckResponse>(result);
                }
                catch (Exception)
                {
                    transferCheckResponse = XMLDeserialize<TransferCheckResponse>(result);
                }

                if (transferCheckResponse.ErrorCode is 3 or 99)
                {
                    return new ApiResult<TransferResult>()
                    {
                        ErrorCode = ErrorCode.TransferFail,
                        RawErrorCode = transferCheckResponse.ErrorCode.ToString(),
                        RawMessage = transferCheckResponse.Desc.ToString(),
                        Data = new TransferResult()
                        {
                            Status = TransferStatus.Fail,
                        }
                    };
                }
                
                return new ApiResult<TransferResult>()
                {
                    ErrorCode = ErrorCode.Success,
                    Data = new TransferResult()
                    {
                        Status = TransferStatus.Success
                    }
                };
            }
            
            return new ApiResult<TransferResult>()
            {
                ErrorCode = ErrorCode.TransferPending,
                Data = new TransferResult()
                { 
                    Status = TransferStatus.Pending,
                }
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = true;
            return transIdModel;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            //改為全拿wager資料
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var spName = "Service_TpWager_HL_Detail_Get";
            var db = DataBase.Wager;
            var detailList = TpBetRecordProvider.DbQuery(spName, new{getBetDetail.TransId}, db);
            var getDetail = detailList.FirstOrDefault();
            var detail = ToJson(detailList);
            
            return new LotteryDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
                RefTransId = getBetDetail.TransId,
                TpUserName = getDetail.GetValueOrDefault("loginID")?.ToString(),
                UTCBetTime = DateTime.Parse(getDetail.GetValueOrDefault("dateTime")?.ToString()),
                UTCSettledTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(getDetail.GetValueOrDefault("SettleTimeUnix").ToString())).DateTime,
                Stake = detailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("bet"))) * tpCurrency.ExchangeRate,
                WinlostAmount = detailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("strike"))) * tpCurrency.ExchangeRate - detailList.Sum(i => Math.Abs((decimal)i.GetValueOrDefault("bet"))) * tpCurrency.ExchangeRate,
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {

            var spName = "Service_TpWager_HL_Detail_Get";
            var db = DataBase.Wager;
            var detailList = TpBetRecordProvider.DbQuery(spName, new{betRecordInfo.TransId}, db);
            
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                RefNo = detailList.GetFirstStringValue("betID"),
                GameID = TpGameType.HaoLong_4D.ToString(),
                Game = GetGameName(detailList.GetFirstStringValue("drawType")),
                Title = tpGameType.TypeName,
                BetType = detailList.GetFirstStringValue("betPosition"),
                BetChoice = detailList.GetFirstStringValue("betNumber"),
                Result = detailList.GetFirstStringValue("drawType"),
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
        }
        
        private bool CheckHaoLongBusyTime(int timeType)
        {
            //1:創建帳號或拉細單
            //2:轉帳行為
            //3:Test
            
            var utcNow = DateTime.UtcNow;
            var gmtPlus8 = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var timeInGmtPlus8 = TimeZoneInfo.ConvertTimeFromUtc(utcNow, gmtPlus8);
            return timeType switch
            {
                1 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(14, 55, 0) &&
                       timeInGmtPlus8.TimeOfDay <= new TimeSpan(20, 5, 0),
                2 => timeInGmtPlus8.TimeOfDay >= new TimeSpan(18, 55, 0) &&
                       timeInGmtPlus8.TimeOfDay <= new TimeSpan(20, 20, 0),
                3 =>  timeInGmtPlus8.TimeOfDay >= new TimeSpan(00, 00, 0) &&
                      timeInGmtPlus8.TimeOfDay <= new TimeSpan(23, 59, 59),
                _ => false
            };
        }

        private string GetGameName(string drawType)
        {
            return drawType switch
            {
                "1" => "MAGNUM",
                "2" => "PMP",
                "3" => "TOTO",
                "4" => "SINGAPORE",
                "5" => "SABAH",
                "6" => "SANDAKAN",
                "7" => "SARAWAK",
                "8" => "GDLOTTO",
                "9" => "9LOTTO",
                _ => "NA"
            };
        }
    }
}