using System.Text;
using System.Web;
using System.Xml.Linq;

using Microsoft.Extensions.DependencyInjection;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.BTI
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, TpSiteConfig> tpSiteConfigs;

        public TpHelper(IServiceProvider provider)
        {
            tpSiteConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<TpSiteConfig>(TpEnum.BTI.ToString());
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var gameUrl = tpSiteConfigs[tpCustInfo.SiteId].GameUrl;
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{gameUrl}/{TpLangId}/"
                };
            }

            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token, tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            var host = inputs.ContainsKey($"Host") && !string.IsNullOrEmpty(inputs[$"Host"])
               ? inputs[$"Host"] : throw new Exception($"host is empty");
            var homeUrl = inputs.ContainsKey($"homeUrl") && !string.IsNullOrEmpty(inputs[$"homeUrl"])
               ? inputs[$"homeUrl"] : "";

            var api = HttpUtility.UrlEncode($"{host}/btisport.js");
            //桌機亞洲版 後面加/vi/asian-view/ 桌機歐洲版 後面加/vi/sports
            var view = (appType == AppType.Desktop) ? "/asian-view" : "";

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{gameUrl}/{TpLangId}{view}?operatorToken={sessionKey}&api={api}&home_url={homeUrl}",
                RawMessage = sessionKey
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var sportsDetail = TpBetRecordProvider.DbQueryFirst("Service_BettransSports_BTI_Detail_Get", new { getBetDetail.TransId }, DataBase.Report);
            if (sportsDetail == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }

            var xd = XDocument.Parse(sportsDetail.GetValueOrDefault("BodyData")?.ToString());
            var selection = xd.Descendants("Selection").FirstOrDefault();
            var detail = ToJson(sportsDetail);
            var (homeyScore, awayScore) = FindScore(selection);
            var (homeyHandicap, awayHandicap, handicap) = FindHandicap(selection);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    TpUserName = sportsDetail.GetValueOrDefault("cust_id")?.ToString(),
                    UTCBetTime = DateTime.Parse(sportsDetail.GetValueOrDefault("TransTime").ToString()),
                    UTCSettledTime = DateTime.Parse(sportsDetail.GetValueOrDefault("SettleTime").ToString()),
                    Stake = (decimal)sportsDetail.GetValueOrDefault("Stake"),
                    WinlostAmount = (decimal)sportsDetail.GetValueOrDefault("WinLostAmount"),
                    RefTransId = sportsDetail.GetValueOrDefault("reserve_id")?.ToString(),

                    Odds = selection.Attribute("OddsInUserStyle").Value,
                    LeagueName = selection.Attribute("LeagueName").Value,
                    SelectionName = selection.Attribute("YourBet").Value,
                    HomeTeamName = selection.Attribute("HomeTeam").Value,
                    AwayTeamName = selection.Attribute("AwayTeam").Value,
                    EventName = selection.Attribute("EventTypeName").Value,
                    TicketStatus = FindTicketStatus(xd),
                    HomeyScore = homeyScore,
                    AwayScore = awayScore,
                    AwayHandicap = awayHandicap,
                    HomeHandicap = homeyHandicap,
                    Handicap = handicap,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransSports_BTI_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var xd = XDocument.Parse(detail.GetValueOrDefault("BodyData")?.ToString());
            var selection = xd.Descendants("Selection").FirstOrDefault();
            var bet = xd.Descendants("Bet").FirstOrDefault();
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = selection.Attribute("BranchID").Value,
                Game = GetGameName(selection.Attribute("BranchName").Value),
                RefNo = detail.GetStringValue("reserve_id"),
                Title = selection.Attribute("EventName").Value,
                MatchID = selection.Attribute("EventID").Value,
                BetType = bet.Attribute("BetType").Value,
                BetChoice = selection.Attribute("YourBet").Value,
                Result = FindTicketStatus(xd),
                DeviceType = DeviceType.None,
                IP = string.Empty,
                Odds = decimal.TryParse(selection.Attribute("OddsInUserStyle").Value, out var odds) ? odds : 0,
                OddsTypeEnum = GetOddsType(selection.Attribute("UserOddStyle").Value),
                Live = selection.Attribute("IsLive").Value,
            };
        }

        private string FindTicketStatus(XDocument XD)
        {
            try
            {
                foreach (XElement item in XD.Descendants("Bet"))
                {
                    //比對狀態不同的回傳ID
                    if (item.Attribute("OldStatus").Value != item.Attribute("NewStatus").Value)
                    {
                        return item.Attribute("NewStatus").Value;
                    }
                }
                //都沒找到使用第一筆找到的
                return XD.Descendants("Bet").FirstOrDefault().Attribute("NewStatus").Value;
            }
            catch (Exception)
            {
                return "";
            }
        }
        private (string homeyScore, string awayScore) FindScore(XElement element)
        {
            try
            {
                var result = element.Attribute("Score").Value.Split(":");
                return (result[0].Trim(), result[1].Trim());
            }
            catch (Exception)
            {
                return ("", "");
            }
        }

        private (string homeyHandicap, string awayHandicap, string handicap) FindHandicap(XElement element)
        {
            var homeyHandicap = string.Empty;
            var awayHandicap = string.Empty;
            var handicap = string.Empty;
            try
            {

                //Marseille (Pipis) -0.25
                //LA Galaxy -0
                //Le Havre +0.5
                var yourBet = element.Attribute("YourBet").Value;
                var homeTeamName = element.Attribute("HomeTeam").Value;
                var awayTeamName = element.Attribute("AwayTeam").Value;

                var plus = yourBet.IndexOf("+");
                if (plus > 0)
                {
                    handicap = yourBet.Substring(plus + 1, yourBet.Length - (plus + 1));
                    if (yourBet.Contains(homeTeamName))
                    {
                        homeyHandicap = $"+{handicap}";
                        awayHandicap = $"-{handicap}";
                    }
                    if (yourBet.Contains(awayTeamName))
                    {
                        awayHandicap = $"+{handicap}";
                        homeyHandicap = $"-{handicap}";
                    }
                }

                var minus = yourBet.IndexOf("-");
                if (minus > 0)
                {
                    handicap = yourBet.Substring(minus + 1, yourBet.Length - (minus + 1));
                    if (yourBet.Contains(homeTeamName))
                    {
                        homeyHandicap = $"-{handicap}";
                        awayHandicap = $"+{handicap}";
                    }
                    if (yourBet.Contains(awayTeamName))
                    {
                        awayHandicap = $"-{handicap}";
                        homeyHandicap = $"+{handicap}";
                    }
                }


            }
            catch (Exception)
            {
                // use default
            }
            return (homeyHandicap, awayHandicap, handicap);
        }

        private OddsTypeEnum GetOddsType(string oddsType)
        {
            return oddsType switch
            {
                "Malay" => OddsTypeEnum.MalayOdds,
                "Hongkong" => OddsTypeEnum.HongKongOdds,
                "Decimal" => OddsTypeEnum.DecimalOdds,
                "Indo" => OddsTypeEnum.IndoOdds,
                _ => OddsTypeEnum.None,
            };
        }

        private static readonly Dictionary<string, SportType> GameNameMappings = new Dictionary<string, SportType>
        {
            { "Soccer", SportType.Soccer },
            { "Basketball", SportType.Basketball },
            { "Badminton", SportType.Badminton },
            { "Tennis", SportType.Tennis },
            { "Volleyball", SportType.Volleyball },
            { "Virtual Soccer", SportType.VirtualSports }
        };

        private static string GetGameName(string gameName)
        {
            return GameNameMappings.TryGetValue(gameName, out var sportType)
                ? Enum.GetName(typeof(SportType), sportType)
                : gameName;
        }
    }
}
