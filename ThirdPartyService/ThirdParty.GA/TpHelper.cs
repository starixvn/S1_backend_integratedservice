using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.GA
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly Dictionary<int, GAConfig> _siteGAConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteGAConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GAConfig>(TpEnum.GA.ToString());
        }
        public class GAConfig : TpSiteConfig
        {
            public string PrivateKey { get; set; }
            public string PartnerAccountID { get; set; }
            public string GameClientID { get; set; }
        }
        public class LoginResponse
        {
            public string token { get; set; }
            public string game_client_url { get; set; }
            public Member member { get; set; }
        }
        public class Member
        {
            public string member_ref_id { get; set; }
            public string display_name { get; set; }
            public string account_id { get; set; }
            public bool enable { get; set; }
            public bool freeze { get; set; }
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
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQuery("Service_TpWager_GA_Get",
                new { sid = getBetDetail.TransId }, DataBase.Wager).FirstOrDefault();

            //jackpot沒有wager資料
            if(wagerDetail == null)
            {
                return new CockFightingDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                    TpUserName = tpCustInfo.TpUserName
                };
            }

            var detail = ToJson(wagerDetail);
            return new CockFightingDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                Detail = detail,
                TpCurrency = tpCurrency,
                TpUserName = wagerDetail.GetValueOrDefault("member_ref_id")?.ToString(),
                UTCBetTime = (DateTime)wagerDetail.GetValueOrDefault("created_date_time"),
                UTCSettledTime = (DateTime)wagerDetail.GetValueOrDefault("settled_date_time"),
                Stake = (decimal)wagerDetail.GetValueOrDefault("bet_amount") * tpCurrency.ExchangeRate,
                WinlostAmount = ((decimal)wagerDetail.GetValueOrDefault("win_loss_amount") * tpCurrency.ExchangeRate) - ((decimal)wagerDetail.GetValueOrDefault("ante_amount") * tpCurrency.ExchangeRate),
                RefTransId = getBetDetail.TransId
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_GA_Get", new { sid = betRecordInfo.TransId }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = tpGameType.TypeName,
                Game = tpGameType.TypeName,
                RefNo = string.Empty,
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("match_day_code"),
                BetType = detail.GetStringValue("selection"),
                BetChoice = string.Empty,
                Result = detail.GetStringValue("result"),
                DeviceType = DeviceType.None,
                IP = string.Empty,
                Odds = (decimal)detail.GetValueOrDefault("odds"),
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            var config = _siteGAConfig[tpCustInfo.SiteId];
            string apiUrl = config.ApiUrl;

            // Header
            Dictionary<string, string> headerReq = new Dictionary<string, string>();
            headerReq.Add("Authorization", config.PrivateKey);

            var sParam = new
            {
                member_ref_id = tpCustInfo.TpUserName,
                display_name = tpCustInfo.TpUserName,
                account_id = config.PartnerAccountID
            };

            var param = ToJson(sParam);
            var result = HttpPost($"{apiUrl}/api/v1/members/login", param, "application/json", headerReq);

            var tpData = JsonConvert.DeserializeObject<LoginResponse>(result);

            if (tpData.member.enable && !tpData.member.freeze)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = tpData.game_client_url;
            }
            else
            {
                var enable = tpData.member.enable ? "Yes" : "No";
                var freeze = tpData.member.freeze ? "Yes" : "No";
                oApiResult.Message = $"enable:{enable}, freeze:{freeze}";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }
    }
}