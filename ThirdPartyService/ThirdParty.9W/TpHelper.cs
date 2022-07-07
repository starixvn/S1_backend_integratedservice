using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using Newtonsoft.Json.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Extensions;

namespace ThirdParty.NineW
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular, IThirdPartyGetBalanceParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);

            var tsCodesObj = new
            {
                userId = tpCustInfo.TpUserName,
                tsCode = TransID
            };

            var tsCodes = JsonConvert.SerializeObject(tsCodesObj);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/checkTransferRecord";
            var sParam = $"cert={config.Cert}&tsCodes={tsCodes}";

            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 300);
            var result = GetCheckTransferRecordResult(TransID, json);

            if (result.status != "1012"
                || !result.TransferResult.Any(d => d.tscode == TransID
                    && d.userId.Equals(tpCustInfo.TpUserName, StringComparison.OrdinalIgnoreCase)
                    && Math.Abs(d.balance) == Amount))
            {
                return new ApiResult<TransferResult>()
                {
                    ErrorCode = ErrorCode.TransferFail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                    Data = new TransferResult()
                    {
                        Status = TransferStatus.Fail
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

        private NineWResult GetCheckTransferRecordResult(string TransID, string json)
        {
            #region 註解

            // Json 的 key 是動態的，依據 TransID 給 key，故用 JObject 方式取值
            //{
            //    "result": {
            //        "test132972571808490060": {
            //            "balance": 1000,
            //            "createdate": "05/17/2022 18:33:01",
            //            "tsCode": "test132972571808490060",
            //            "userId": "xnova88249951",
            //            "status": "1012"
            //        }
            //    },
            //    "status": "1"
            //}

            #endregion

            var response = JObject.Parse(json);
            var responseResult = response.GetValue("result").Value<JObject>();
            var item = responseResult.GetValue(TransID).Value<JObject>();

            var result = new NineWResult()
            {
                TransferResult = new List<NineWTransfer>()
                {
                    new NineWTransfer()
                    {
                        userId = item.GetValue("userId").Value<string>(),
                        tscode = item.GetValue("tsCode").Value<string>(),
                        balance = item.GetValue("balance").Value<decimal>(),
                    }
                },
                status = item.GetValue("status").Value<string>()
            };
            return result;
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
            _ = GetKey(tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, config);
            // 暫時加上，9w 限制 1 time pre sec(userid)
            Thread.Sleep(1000);
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/getBalance";
            var sParam = $"cert={config.Cert}&alluser=0&userIds={tpCustInfo.TpUserName}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult<UserBalance>()
            {
                ErrorCode = ErrorCode.Success,
                Message = "ok",
                Data = new UserBalance()
                {
                    balance = result.BalaceResult[0].balance,
                }
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new TransIdModel
            {
                Implemented = false
            };
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.UserNotLogin,
                    Message = "Not support fun game.",
                };
            }

            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = JsonConvert.SerializeObject(new
                {
                    url = $"{config.OldApiUrl}/player/{config.WebSite}/login",
                    key = GetKey(tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, config),
                    userId = tpCustInfo.TpUserName,
                })
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/deposit";
            var sParam = $"cert={config.Cert}&userId={tpCustInfo.TpUserName}&balance={Amount}&tsCode={TransID}";

            if (direction == Direction.ToMain)
            {
                apiUrl = $"{config.ApiUrl}/{config.WebSite}/withdraw";
                sParam = $"cert={config.Cert}&userId={tpCustInfo.TpUserName}&withdrawtype=0&balance={Amount}&tsCode={TransID}";
            }

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);
            if (result.status != "1")
            {
                return new ApiResult<TransferResult>()
                {
                    ErrorCode = ErrorCode.TransferPending,
                    RawErrorCode = result.status,
                    RawMessage = json,
                    Data = new TransferResult()
                    {
                        Status = TransferStatus.Pending
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

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_9W_Detail_Get", new { getBetDetail.TransId }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Sports => new SportsDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    TpUserName = wagerDetail.GetValueOrDefault("userId")?.ToString(),
                    UTCBetTime = DateTime.Parse(wagerDetail.GetValueOrDefault("betPlaced").ToString()),
                    UTCSettledTime = DateTime.Parse(wagerDetail.GetValueOrDefault("matchSettledDate").ToString()),
                    Odds = wagerDetail.GetValueOrDefault("matchOddsReq")?.ToString(),
                    Stake = (decimal)wagerDetail.GetValueOrDefault("matchStake") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)wagerDetail.GetValueOrDefault("profitLoss") * tpCurrency.ExchangeRate,
                    LeagueName = wagerDetail.GetValueOrDefault("eventTypeName")?.ToString(),
                    TicketStatus = wagerDetail.GetValueOrDefault("betStatus")?.ToString(),
                    RefTransId = wagerDetail.GetValueOrDefault("betId")?.ToString(),
                    EventName = wagerDetail.GetValueOrDefault("eventName")?.ToString(),
                    SelectionName = wagerDetail.GetValueOrDefault("selectionName")?.ToString(),
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_9W_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

            return tpGameType.CategoryId switch
            {
                TpCategory.Sports => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detail.GetStringValue("eventType"),
                    Game = detail.GetStringValue("eventTypeName"),
                    RefNo = detail.GetStringValue("betId"),
                    Title = detail.GetStringValue("eventName"),
                    MatchID = detail.GetStringValue("eventId"),
                    BetType = detail.GetStringValue("matchType"),
                    BetChoice = detail.GetStringValue("selectionName"),
                    Result = detail.GetStringValue("betStatus"),
                    Odds = decimal.TryParse(detail.GetStringValue("matchOddsReq"), out var odds) ? odds : 0,
                    OddsTypeEnum = GetOddsType(detail.GetStringValue("oddsType")),
                    DeviceType = DeviceType.None,
                    Live = string.Empty,
                    IP = detail.GetStringValue("ip"),
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = new NineWConfig(inputs, tpCustInfo.TpCurrencyId);
            var apiUrl = $"{config.OldApiUrl}/player/{config.WebSite}/logout";
            var sParam = $"cert={config.Cert}&userIds={tpCustInfo.TpUserName}";

            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        private string GetKey(string user, string currency, NineWConfig config)
        {
            var apiUrl = $"{config.OldApiUrl}/player/{config.WebSite}/getKey";
            var sParam = $"&cert={config.Cert}&userId={user}&userName={user}&agent={config.Agent}&currency={currency}";
            var json = HttpPost(apiUrl, sParam, ContentType);
            var result = FromJson<NineWResult>(json);
            return result.status == "1"
                ? HttpUtility.UrlEncode(result.key) : throw new Exception($"9w get key error status is : {result.status}");
        }

        /// <summary>
        /// 取得指定客戶的餘額資料
        /// </summary>
        /// <param name="inputs">第三方相關參數</param>
        /// <param name="tpUserNameString">第三方使用者名稱參數清單</param>
        /// <param name="tpCurrencyId">第三方幣別</param> 
        /// <returns></returns>
        public ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString, string tpCurrencyId)
        {
            var config = new NineWConfig(inputs, tpCurrencyId);
            var apiUrl = $"{config.ApiUrl}/{config.WebSite}/getBalance";
            var sParam = $"cert={config.Cert}&alluser=0&userIds={tpUserNameString}";

            var json = HttpPost(apiUrl, sParam, ContentType, timeOutTime: 120);
            var result = FromJson<NineWResult>(json);

            if (result.status != "1")
            {
                return new ApiResult<List<ReceiveTpUserBalanceModel>>()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = "fail",
                    RawErrorCode = result.status,
                    RawMessage = json,
                };
            }

            return new ApiResult<List<ReceiveTpUserBalanceModel>>()
            {
                ErrorCode = ErrorCode.Success,
                Message = "ok",
                Data = result.BalaceResult.Select(x => new ReceiveTpUserBalanceModel
                {
                    TpUserName = x.userId,
                    Balance = x.balance,
                    ErrorCode = ErrorCode.Success
                }).ToList()
            };
        }

        private class NineWConfig
        {
            public string ApiUrl { get; set; }
            public string OldApiUrl { get; set; }
            public string WebSite { get; set; }
            public string Cert { get; set; }
            public string Agent { get; set; }

            public NineWConfig(Dictionary<string, string> inputs, string tpCurrencyId)
            {
                ApiUrl = inputs.ContainsKey($"ApiUrl") && !string.IsNullOrEmpty(inputs[$"ApiUrl"])
                ? inputs[$"ApiUrl"] : throw new Exception($"ApiHost is empty");
                WebSite = inputs.ContainsKey($"website") && !string.IsNullOrEmpty(inputs[$"website"])
                ? inputs[$"website"] : throw new Exception($"website is empty");
                Cert = inputs.ContainsKey($"cert") && !string.IsNullOrEmpty(inputs[$"cert"])
                ? inputs[$"cert"] : throw new Exception($"cert is empty");
                Agent = inputs.ContainsKey($"agent_{tpCurrencyId}") && !string.IsNullOrEmpty(inputs[$"agent_{tpCurrencyId}"])
                ? inputs[$"agent_{tpCurrencyId}"] : throw new Exception($"agent {tpCurrencyId} is empty");
                OldApiUrl = inputs.ContainsKey($"OldApiUrl") && !string.IsNullOrEmpty(inputs[$"OldApiUrl"])
                    ? inputs[$"OldApiUrl"] : throw new Exception($"OldApiHost is empty");
            }
        }

        private class NineWResult
        {
            public string status { get; set; }
            public string key { get; set; }

            [JsonProperty("results")]
            public List<NineWBalance> BalaceResult { get; set; }

            [JsonProperty("result")]
            public List<NineWTransfer> TransferResult { get; set; }
        }

        private class NineWBalance
        {
            public string userId { get; set; }
            public decimal balance { get; set; }
            public decimal exposure { get; set; }
        }

        private class NineWTransfer
        {
            public string tscode { get; set; }
            public string userId { get; set; }
            public decimal balance { get; set; }
            public string createdate { get; set; }
        }

        private OddsTypeEnum GetOddsType(string oddsType)
        {
            return oddsType switch
            {
                "0" => OddsTypeEnum.DecimalOdds,
                "1" => OddsTypeEnum.IndianOdds,
                _ => OddsTypeEnum.None,
            };
        }

    }
}
