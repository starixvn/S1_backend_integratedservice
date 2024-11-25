using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PNG
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, Dictionary<string, string>> siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.PNG).ToDictionary(d => d.AppId, d => d.Name);
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.PNG.ToString());
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            // PlayNGo 沒有提供會員註冊 API
            var oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success
            };
            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiResult = new ApiResult();

            try
            {
                var siteInputs = siteConfigDic[tpCustInfo.SiteId];
                var config = new PNGConfig(siteInputs);
                var token = ApplyToken(tpCustInfo);
                var channel = appType == AppType.Desktop
                    ? "desktop"
                    : "mobile";

                var originUrl = inputs["homeUrl"];

                var dict = new Dictionary<string, string>()
                {
                    { "gid", inputs["gameId"] },
                    { "lang", TpLangId },
                    { "pid", config.Pid.ToString() },
                    { "practice","0"},// 0 真錢
                    { "ticket", token },
                    { "channel", channel },
                    { "origin", originUrl },
                };

                var queryString = dict.ToQueryString();
                var launcherGameUrlResult = $"{config.ApiUrl}{config.GameUrl}?{queryString}";

                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = launcherGameUrlResult;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = "PNG Login Fail";
            }

            return apiResult;
        }

        /// <summary>
        /// 用三方 API 去拿 RoundId 內容連結
        /// </summary>
        /// <param name="getBetDetail"></param>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            var config = new PNGConfig(siteInputs);
            var betDetailUrl = $"{config.ApiUrl}{config.GameHistoryDesktop}{getBetDetail.TransId}";

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = betDetailUrl,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        /// <summary>
        /// 沒有 Wager 拿 BetTrans 資料當細單
        /// </summary>
        /// <param name="betRecordInfo"></param>
        /// <param name="tpGameType"></param>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_PNG_Detail_Get", new { TransId = betRecordInfo.TransId }, DataBase.Main);

            if (!detail.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameId = detail.GetStringValue("GameId");
            var roundId = detail.GetStringValue("RoundId");
            var channel = detail.GetStringValue("Channel");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameId,
                Game = GetGameName(gameId),
                RefNo = roundId,
                Title = tpGameType.TypeName,
                MatchID = roundId,
                BetType = string.Empty,
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = GetDeviceType(channel),
                IP = string.Empty,
            };
        }

        private string GetGameName(string gameId)
        {
            if (!_gameNameList.TryGetValue(gameId, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic != null && dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        private DeviceType GetDeviceType(string channel)
        {
            return channel switch
            {
                "1" => DeviceType.Desktop,
                "2" => DeviceType.Mobile,
                _ => DeviceType.None
            };
        }

        #region NotImplemented

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID,
            string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }



        #endregion
    }
}
