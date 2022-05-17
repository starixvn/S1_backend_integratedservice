using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.BTI
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
            var GameUrl = inputs.ContainsKey($"GameUrl") && !string.IsNullOrEmpty(inputs[$"GameUrl"]) 
                ? inputs[$"GameUrl"] : throw new Exception($"GameUrl is empty");
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = $"{GameUrl}/{TpLangId}/"
                };
            }
            
            var key = ApplyToken(tpCustInfo);
            inputs.TryGetValue("Token", out string token);
            var sessionKeyArr = new string[] { key, token , tpCustInfo.TpUserName };
            var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
            var sessionKey = HttpUtility.UrlEncode(Convert.ToBase64String(bytes));
            var host = inputs.ContainsKey($"Host") && !string.IsNullOrEmpty(inputs[$"Host"])
               ? inputs[$"Host"] : throw new Exception($"host is empty");
            var homeUrl = inputs.ContainsKey($"homeUrl") && !string.IsNullOrEmpty(inputs[$"homeUrl"])
               ? inputs[$"homeUrl"] : "";
            var api = HttpUtility.UrlEncode($"{host}/btisport.js");
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{GameUrl}/{TpLangId}/?operatorToken={sessionKey}&api={api}&home_url={homeUrl}",
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
            var sportsDetail = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransSports_BTI_Detail_Get", new { getBetDetail.TransId });
            var xd = XDocument.Parse(sportsDetail.GetValueOrDefault("BodyData")?.ToString());
            var selection = xd.Descendants("Selection").FirstOrDefault();
            var detail = ToJson(sportsDetail);
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

                    Odds = selection.Attribute("DecimalOdds").Value,
                    LeagueName = selection.Attribute("LeagueName").Value,
                    SelectionName = selection.Attribute("YourBet").Value,
                    HomeTeamName = selection.Attribute("HomeTeam").Value,
                    AwayTeamName = selection.Attribute("AwayTeam").Value,                
                    EventName = selection.Attribute("EventTypeName").Value,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }
    }
}
