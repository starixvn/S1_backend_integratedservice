using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.IM
{
    public partial class TpHelper : ThirdPartyHelperBase
    {
        private const string ContentType = "application/json";
        private readonly WebProxy _proxy;
        

        public TpHelper(IServiceProvider provider)
        {
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            if (env.EnvironmentName == "Development")
            {
                var proxyHost = configuration.GetValue<string>("configProxyHost");
                var proxyPort = configuration.GetValue<int>("configProxyPort");
                _proxy = new WebProxy(proxyHost, proxyPort);
            }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = $"{inputs["ApiUrl"]}/Player/Register";
            // var prefix = inputs.ContainsKey("Prefix") && !string.IsNullOrEmpty(inputs["Prefix"]) ? inputs["Prefix"] : throw new Exception("Prefix");
            // var playerName = $"{prefix}_{tpCustInfo.TpUserName}";
            
            var signUp = new RegisterModel
            {
                MerchantCode = inputs["Key"],
                PlayerId = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId,
                Password = tpCustInfo.TpPwd
            };
            
            var json = HttpPostWithProxy(apiUrl, ToJson(signUp) , ContentType ,_proxy);
            var result = FromJson<RegisterResponseModel>(json);

            if (result.Code == 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = result.Message
                };
            }
            else
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = result.Message
                };
            }
        }
        
        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = $"{inputs["ApiUrl"]}/Game/NewLaunchGame";
            // var prefix = inputs.ContainsKey("Prefix") && !string.IsNullOrEmpty(inputs["Prefix"]) ? inputs["Prefix"] : throw new Exception("Prefix");
             var playerName = tpCustInfo.TpUserName;
            
            var tpGameType = inputs.ContainsKey("tpGameType") && !string.IsNullOrEmpty(inputs["tpGameType"]) ? Convert.ToInt32(inputs["tpGameType"]) : (int)TpGameType.IM_Sports;
            var productWallet = tpGameType switch
            {
                (int) TpGameType.IM_Sports => (int) ProductWallet.IMSB,
                (int) TpGameType.IM_ESports => (int) ProductWallet.ESportsbull,
                (int) TpGameType.SPRIBE => (int) ProductWallet.SPRIBE ,
                _ => 0
            };
            
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                apiUrl = $"{inputs["ApiUrl"]}/Game/LaunchFreeGame";
                playerName = null;
            }
            
            var signUp = new LaunchGameModel
            {
                MerchantCode = inputs["Key"],
                PlayerId = playerName,
                GameCode = inputs["gameId"],
                Language = TpLangId,
                IpAddress = inputs["ClientIP"],
                ProductWallet = productWallet,
                LobbyURL = inputs["homeUrl"]
            };
            
            var json = HttpPostWithProxy(apiUrl, ToJson(signUp) , ContentType, _proxy , null , 0);
            var result = FromJson<LaunchGameResponseModel>(json);
            
            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = result.GameUrl,
                RawMessage = result.Message
            };
        }
        
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }
        
        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            // var tpCurrency = new TpCurrency()
            // {
            //     ExchangeRate = tpCustInfo.ExchangeRate,
            //     TpCurrencyId = tpCustInfo.TpCurrencyId,
            // };
            // var sportsDetail = TpBetRecordProvider.DbQueryFirst("Service_BettransSports_BTI_Detail_Get", new { getBetDetail.TransId }, DataBase.Report);
            // if (sportsDetail == null)
            // {
            //     return new BaseDetail(getBetDetail.TpGameTypeOption)
            //     {
            //         TransId = getBetDetail.TransId,
            //         TpCurrency = tpCurrency,
            //     };
            // }
            //
            // var xd = XDocument.Parse(sportsDetail.GetValueOrDefault("BodyData")?.ToString());
            // var selection = xd.Descendants("Selection").FirstOrDefault();
            // var detail = ToJson(sportsDetail);
            // var (homeyScore, awayScore) = FindScore(selection);
            // var (homeyHandicap, awayHandicap, handicap) = FindHandicap(selection);
            // return getBetDetail.TpGameTypeOption.CategoryId switch
            // {
            //     TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
            //     {
            //         TransId = getBetDetail.TransId,
            //         Detail = detail,
            //         TpCurrency = tpCurrency,
            //         TpUserName = sportsDetail.GetValueOrDefault("cust_id")?.ToString(),
            //         UTCBetTime = DateTime.Parse(sportsDetail.GetValueOrDefault("TransTime").ToString()),
            //         UTCSettledTime = DateTime.Parse(sportsDetail.GetValueOrDefault("SettleTime").ToString()),
            //         Stake = (decimal)sportsDetail.GetValueOrDefault("Stake"),
            //         WinlostAmount = (decimal)sportsDetail.GetValueOrDefault("WinLostAmount"),
            //         RefTransId = sportsDetail.GetValueOrDefault("reserve_id")?.ToString(),
            //
            //         Odds = selection.Attribute("OddsInUserStyle").Value,
            //         LeagueName = selection.Attribute("LeagueName").Value,
            //         SelectionName = selection.Attribute("YourBet").Value,
            //         HomeTeamName = selection.Attribute("HomeTeam").Value,
            //         AwayTeamName = selection.Attribute("AwayTeam").Value,
            //         EventName = selection.Attribute("EventTypeName").Value,
            //         TicketStatus = FindTicketStatus(xd),
            //         HomeyScore = homeyScore,
            //         AwayScore = awayScore,
            //         AwayHandicap = awayHandicap,
            //         HomeHandicap = homeyHandicap,
            //         Handicap = handicap,
            //     },
            //     _ => new BaseDetail(getBetDetail.TpGameTypeOption)
            //     {
            //         TransId = getBetDetail.TransId,
            //         Detail = detail,
            //         TpCurrency = tpCurrency,
            //     }
            // };
            return null;
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
        //     var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransSports_BTI_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
        //     if (detail == null)
        //     {
        //         return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
        //     }
        //
        //     var xd = XDocument.Parse(detail.GetValueOrDefault("BodyData")?.ToString());
        //     var selection = xd.Descendants("Selection").FirstOrDefault();
        //     var bet = xd.Descendants("Bet").FirstOrDefault();
        //     return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
        //     {
        //         GameID = selection.Attribute("BranchID").Value,
        //         Game = selection.Attribute("BranchName").Value,
        //         RefNo = detail.GetStringValue("reserve_id"),
        //         Title = selection.Attribute("EventName").Value,
        //         MatchID = selection.Attribute("EventID").Value,
        //         BetType = bet.Attribute("BetType").Value,
        //         BetChoice = selection.Attribute("YourBet").Value,
        //         Result = FindTicketStatus(xd),
        //         DeviceType = DeviceType.None,
        //         IP = string.Empty,
        //         Odds = decimal.TryParse(selection.Attribute("OddsInUserStyle").Value, out var odds) ? odds : 0,
        //         OddsTypeEnum = GetOddsType(selection.Attribute("UserOddStyle").Value),  
        //         Live = selection.Attribute("IsLive").Value,
        //     };
        
            return null;
        }

     
    }
}
