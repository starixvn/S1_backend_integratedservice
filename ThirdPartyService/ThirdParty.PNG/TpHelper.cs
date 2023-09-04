using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.PNG
{
    public class TpHelper : ThirdPartyHelperBase
    {
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
                var config = new PNGConfig(inputs);
                var token = ApplyToken(tpCustInfo);
                var channel = appType == AppType.Desktop
                    ? "desktop"
                    : "mobile";

                var originUrl = inputs["homeUrl"];

                var dict = new Dictionary<string, string>()
                {
                    { "gid", config.GameId },
                    { "lang", TpLangId },
                    { "pid", config.Pid.ToString() }, 
                    { "practice","0"},// 0 真錢
                    { "ticket", token },
                    { "channel", channel },
                    { "origin", originUrl },
                };

                var queryString = dict.ToQueryString();
                var launcherGameUrlResult = $"{config.RootUrl}{config.GameLaunchUrl}?{queryString}";

                logger.LogInformation($"launcherGameUrlResult:{launcherGameUrlResult}");

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

        #region NotImplemented

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
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

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo,
            Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
