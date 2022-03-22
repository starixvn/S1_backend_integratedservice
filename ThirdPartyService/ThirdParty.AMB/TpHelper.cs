using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;

namespace ThirdParty.AMB
{
    public class TpHelper : ThirdPartyHelperBase
    {
        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult();
            var ok = false;
            if (inputs.ContainsKey("ApiUrl") && inputs.ContainsKey("Agent") && inputs.ContainsKey("SecretKey"))
            {
                var ambPoker = AMBFactory.Create(TpGameType.AMB_Poker, inputs);
                var pokerResult = ambPoker.CreateMember(tpCustInfo.TpUserName, HttpPost);
                if (pokerResult.status.code != 0 && pokerResult.status.code != 902)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = pokerResult.status.message;
                    oApiResult.RawErrorCode = pokerResult.status.code.ToString();
                    return oApiResult;
                }
                ok = true;
            }

            if (inputs.ContainsKey("SlotApiUrl") && inputs.ContainsKey("SlotAgent") && inputs.ContainsKey("SlotSecretKey"))
            {
                var ambSlot = AMBFactory.Create(TpGameType.AMB_RNG, inputs);
                var slotResult = ambSlot.CreateMember(tpCustInfo.TpUserName, HttpPost);
                if (slotResult.status.code != 0 && slotResult.status.code != 902)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = slotResult.status.message;
                    oApiResult.RawErrorCode = slotResult.status.code.ToString();
                    return oApiResult;
                }
                ok = true;
            }
            
            if(ok)
                oApiResult.ErrorCode = ErrorCode.Success;
            
            return oApiResult;
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
            var tpGameType = GetInputValue(inputs, "tpGameType");
            var gameId = GetInputValue(inputs, "gameId");
            var homeUrl = GetInputValue(inputs, "homeUrl", true);
            var isFun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) && inputs["fun"] == "1";
            var enumGameType = (TpGameType)int.Parse(tpGameType);
            var amb = AMBFactory.Create(enumGameType, inputs);
            var result = amb.LaunchGame(tpCustInfo.TpUserName, gameId, homeUrl, isFun, HttpPost);

            var oApiResult = new ApiResult();
            if (result.status.code == 0)
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{result.data.url}";
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = result.status.message;
                oApiResult.RawErrorCode = result.status.code.ToString();
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpGameType = (TpGameType)getBetDetail.TpGameTypeOption.Type;
            var amb = AMBFactory.Create(tpGameType, inputs);

            var roundId = "";
            var gameId = "";
            switch (tpGameType)
            {
                case TpGameType.AMB_Poker:
                    roundId = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransPK_AMB_Detail_Get", new {getBetDetail.TransId}).GetValueOrDefault("roundId").ToString();
                    break;
                case TpGameType.AMB_RNG:
                {
                    var detail = TpBetRecordProvider.ReportDbQueryFirst("Service_BettransRng_AMB_Detail_Get",
                        new {YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId});
                    roundId = detail?.GetValueOrDefault("transactionId")?.ToString();
                    gameId = detail?.GetValueOrDefault("gameId")?.ToString();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(tpGameType), tpGameType, null);
            }

            var gameReport = amb.GetRoundResult(tpCustInfo.TpUserName, roundId, gameId, HttpPost);
            if (gameReport.status.code != 0)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {gameReport.status.code} | {gameReport.status.message}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = gameReport.data.url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        private string GetInputValue(Dictionary<string, string> inputs, string key, bool allowNull = false)
        {
            if(inputs.ContainsKey(key) && !string.IsNullOrEmpty(inputs[key]))
            {
                return inputs[key];
            }

            if (allowNull)
                return "";
            throw new Exception($"inputs {key} is empty");
        }
    }
}
