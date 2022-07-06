using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Model.TpMerchantBalance.Response;

namespace ThirdParty.PT
{
    public partial class TpHelper : ThirdPartyHelperBase
    {
        private readonly string storagePath = Path.Combine(ContentRootPath, "Storage", "PT");

        private readonly Dictionary<string, int> timeZoneList = new Dictionary<string, int>() {
            {"MYR",8},
            {"CNY",8},
            {"IDR",8},
            {"THB",7},
            {"VND",7},
            {"INR",5},
            {"USD",0},
        };

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var currency = tpCustInfo.TpCurrencyId;
            var apiUrl = GetApiUrl(inputs);
            var adminName = GetAdminName(inputs, currency);
            var entityKey = GetEntityKey(inputs, currency);
            var pass = GetPass(inputs, currency);
            var prefix = inputs.ContainsKey("Prefix") && !string.IsNullOrEmpty(inputs["Prefix"]) ? inputs["Prefix"] : throw new Exception("Prefix");
            apiUrl = $"{apiUrl}/player/create";

            var header = GetHeader(entityKey);
            var certificate = GetCertificate(currency, pass);
            var playerName = $"{prefix}_{tpCustInfo.TpUserName}";

            var signUp = new
            {
                playername = playerName.ToUpper(),
                adminname = adminName,
                password = tpCustInfo.TpPwd,
                custom02 = prefix
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(signUp));

            var result = HttpPostWithX509(
                apiUrl,
                dicP,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error ?? "";
                    oApiResult.RawErrorCode = error.errorcode.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<CreateMemberResponse>(result);
                    oApiResult.ErrorCode = ErrorCode.Success;
                    tpCustInfo.TpUserName = playerName.ToUpper();
                    oApiResult.RawMessage = "";
                    oApiResult.RawErrorCode = "";
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }

            return oApiResult;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            var client = "";
            if (gameId == "0")
            {
                // live casino
                gameId = "";
                client = appType == AppType.Desktop ? "live_desk" : "live_mob";
            }
            else
            {
                // slot
                client = appType == AppType.Desktop ? "ngm_desktop" : "ngm_mobile";
            }

            if (fun.Equals("0", StringComparison.OrdinalIgnoreCase))
            {
                // change password (error:2) - will also clean lock minutes
                ChangePassword(tpCustInfo, inputs);
            }

            var oApiResult = new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = JsonConvert.SerializeObject(new
                {
                    account = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "guest" : tpCustInfo.TpUserName,
                    password = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "guest" : tpCustInfo.TpPwd,
                    game = gameId,
                    lang = TpLangId,
                    client = client,
                    mode = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "offline" : "real"
                })
            };
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var currency = tpCustInfo.TpCurrencyId;
            var apiUrl = GetApiUrl(inputs);
            var entityKey = GetEntityKey(inputs, currency);
            var pass = GetPass(inputs, currency);
            apiUrl = $"{apiUrl}/player/balance";

            var header = GetHeader(entityKey);
            var certificate = GetCertificate(currency, pass);

            var balance = new
            {
                playername = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(balance));

            var result = HttpPostWithX509(
                apiUrl,
                dicP,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            var oApiResult = new ApiResult<UserBalance>();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error ?? "";
                    oApiResult.RawErrorCode = error.errorcode.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<BalanceResponse>(result);
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Data.balance = json.result.balance;
                }
            }
            else
            {
                oApiResult.RawMessage = result;
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            var currency = tpCustInfo.TpCurrencyId;
            var apiUrl = GetApiUrl(inputs);
            var adminName = GetAdminName(inputs, currency);
            var entityKey = GetEntityKey(inputs, currency);
            var pass = GetPass(inputs, currency);

            var transUrl = $"{apiUrl}/player/deposit";

            var header = GetHeader(entityKey);
            var certificate = GetCertificate(currency, pass);

            var transfer = new DepositParam
            {
                playername = tpCustInfo.TpUserName,
                amount = Math.Floor(Amount * 100) / 100,
                adminname = adminName,
                externaltranid = TransID
            };

            if (direction == Direction.ToMain)
            {
                transUrl = $"{apiUrl}/player/withdraw";
                transfer = new WithdrawParam
                {
                    playername = tpCustInfo.TpUserName,
                    amount = Math.Floor(Amount * 100) / 100,
                    adminname = adminName,
                    isForce = false,
                    externaltranid = TransID,
                    losebonus = false
                };
            }



            var dicP = FromJson<Dictionary<string, string>>(ToJson(transfer));

            var result = HttpPostWithX509(
                transUrl,
                dicP,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            var apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
                    #region 註解Code
                    /*
                      {
                          "code": 71,
                          "description": "Could not load player data. Error: 'UMS API return error on access request attempt'"
                      },
                      {
                          "code": 72,
                          "description": "Could not load player data. Error: 'Service error accessing UMS API'"
                      },
                      {
                          "code": 73,
                          "description": "Could not load player data from database. Error: 'Database error occured, please contact support'. Please, try again later"
                      },
                      {
                          "code": 301,
                          "description": "API error."
                      }
                   */
                    #endregion

                    int[] pend = { 71, 72, 73, 301 };
                    if (pend.Contains(error.errorcode))
                    {
                        apiResult.RawMessage = error.error;
                        apiResult.RawErrorCode = error.errorcode.ToString();
                        apiResult.ErrorCode = ErrorCode.TransferPending;
                        apiResult.Data.Status = TransferStatus.Pending;
                    }
                    else
                    {
                        CheckErrorCodeWithSendNotification(tpCustInfo, Amount, error);
                        apiResult.RawMessage = error.error;
                        apiResult.RawErrorCode = error.errorcode.ToString();
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<TransferResponse>(result);
                    if (json.result.externaltransactionid == TransID)
                    {
                        apiResult.ErrorCode = ErrorCode.Success;
                        apiResult.Data.Status = TransferStatus.Success;
                    }
                    else
                    {
                        apiResult.RawMessage = "No Result";
                        apiResult.RawErrorCode = "1";
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                }
            }
            else
            {
                apiResult.RawMessage = "No Result";
                apiResult.RawErrorCode = "1";
                apiResult.ErrorCode = ErrorCode.TransferPending;
                apiResult.Data.Status = TransferStatus.Pending;
            }

            return apiResult;
        }



        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            try
            {
                var currency = tpCustInfo.TpCurrencyId;
                var apiUrl = GetApiUrl(inputs);
                var entityKey = GetEntityKey(inputs, currency);
                var pass = GetPass(inputs, currency);
                apiUrl = $"{apiUrl}/player/checktransaction";

                var header = GetHeader(entityKey);
                var certificate = GetCertificate(currency, pass);

                var checkTransfer = new
                {
                    externaltransactionid = TransID,
                };

                var dicP = FromJson<Dictionary<string, string>>(ToJson(checkTransfer));

                var result = HttpPostWithX509(
                    apiUrl,
                    dicP,
                    "application/x-www-form-urlencoded",
                    header,
                    certificate
                );

                var apiResult = new ApiResult<TransferResult>();
                if (IsJSON(result))
                {
                    var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                    if (error.error != null)
                    {
                        apiResult.RawMessage = error.error ?? "";
                        apiResult.RawErrorCode = error.errorcode.ToString();
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                    }
                    else
                    {
                        var json = JsonConvert.DeserializeObject<CheckTransferResponse>(result);
                        if (json.result.status == "approved")
                        {
                            apiResult.ErrorCode = ErrorCode.Success;
                            apiResult.Data.Status = TransferStatus.Success;
                        }
                        else if (json.result.status == "declined" || json.result.status == "notallowed" || json.result.status == "missing")
                        {
                            apiResult.RawMessage = json.result.status;
                            apiResult.RawErrorCode = json.result.ToString();
                            apiResult.ErrorCode = ErrorCode.Fail;
                            apiResult.Data.Status = TransferStatus.Fail;
                        }
                        else
                        {
                            // missing 、 waiting
                            apiResult.RawMessage = json.result.status;
                            apiResult.RawErrorCode = json.result.ToString();
                            apiResult.ErrorCode = ErrorCode.TransferPending;
                            apiResult.Data.Status = TransferStatus.Pending;
                        }
                    }
                }
                else
                {
                    apiResult.RawMessage = "No Result";
                    apiResult.RawErrorCode = "1";
                    apiResult.ErrorCode = ErrorCode.TransferPending;
                    apiResult.Data.Status = TransferStatus.Pending;
                }

                return apiResult;
            }
            catch
            {
                throw new Exception($"storagePath={storagePath}");
            }
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel
            {
                Implemented = false
            };
            return transIdModel;
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                int timeSpan = timeZoneList[tpCustInfo.TpCurrencyId];
                int minuteSpan = tpCustInfo.TpCurrencyId == "INR" ? 30 : 0;
                var currency = tpCustInfo.TpCurrencyId;
                var apiUrl = GetApiUrl(inputs);
                var entityKey = GetEntityKey(inputs, currency);
                var pass = GetPass(inputs, currency);

                apiUrl = $"{apiUrl}/player/games";

                var header = GetHeader(entityKey);
                var certificate = GetCertificate(currency, pass);

                var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_PT_Detail_Get",
                   new { getBetDetail.TransId, GameType = getBetDetail.TpGameTypeOption.Type }, DataBase.Wager);
                var sParam = new Dictionary<string, string>
                {
                    { "playername", wagerDetail.GetValueOrDefault("PLAYERNAME").ToString()},
                    { "gametype", wagerDetail.GetValueOrDefault("SHORTNAME").ToString() },
                    { "startdate", DateTime.Parse( wagerDetail.GetValueOrDefault("GAMEDATE").ToString()).AddHours(timeSpan).AddMinutes(minuteSpan).ToString("yyyy-MM-dd HH:mm:ss") },
                    { "enddate", DateTime.Parse( wagerDetail.GetValueOrDefault("GAMEDATE").ToString()).AddHours(timeSpan).AddMinutes(minuteSpan).ToString("yyyy-MM-dd HH:mm:ss") },
                    { "currency", wagerDetail.GetValueOrDefault("CURRENCYCODE").ToString() }
                };

                var json = HttpPostWithX509(
                    apiUrl,
                    sParam,
                    "application/x-www-form-urlencoded",
                    header,
                    certificate
                );
                var result = FromJson<DetailResult>(json);
                var data = result.result.First();
                if (!result.result.Any() && !string.IsNullOrEmpty(data.URL))
                {
                    throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found.");
                }

                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    DetailUrl = data.URL,
                    TpCurrency = new TpCurrency()
                    {
                        ExchangeRate = tpCustInfo.ExchangeRate,
                        TpCurrencyId = tpCustInfo.TpCurrencyId,
                    },
                };
            }
            catch (Exception e)
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {e.Message}");
            }
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_PT_Detail_Get",
                new { betRecordInfo.TransId, GameType = tpGameType.Type }, DataBase.Wager);
            return tpGameType.CategoryId switch
            {
                TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = wagerDetail.GetStringValue("SHORTNAME"),
                    Game = wagerDetail.GetStringValue("GAMENAME"),
                    RefNo = wagerDetail.GetStringValue("GAMECODE"),
                    Title = wagerDetail.GetStringValue("GAMETYPE"),
                    MatchID = wagerDetail.GetStringValue("GAMEID"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = wagerDetail.GetStringValue("INFO"),
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = wagerDetail.GetStringValue("SHORTNAME"),
                    Game = wagerDetail.GetStringValue("GAMENAME"),
                    RefNo = wagerDetail.GetStringValue("GAMECODE"),
                    Title = wagerDetail.GetStringValue("GAMETYPE"),
                    MatchID = wagerDetail.GetStringValue("GAMEID"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = wagerDetail.GetStringValue("INFO"),
                    DeviceType = DeviceType.None,
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private ApiResult ChangePassword(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var currency = tpCustInfo.TpCurrencyId;
            var apiUrl = GetApiUrl(inputs);
            var entityKey = GetEntityKey(inputs, currency);
            var pass = GetPass(inputs, currency);
            apiUrl = $"{apiUrl}/player/changepassword";

            var header = GetHeader(entityKey);
            var certificate = GetCertificate(currency, pass);

            var changePassword = new
            {
                playername = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd,
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(changePassword));

            var result = HttpPostWithX509(
                apiUrl,
                dicP,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            var oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error ?? "";
                    oApiResult.RawErrorCode = error.errorcode.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<ChangePasswordResponse>(result);
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.Message = json.result.password;
                    oApiResult.RawMessage = "";
                    oApiResult.RawErrorCode = "";
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }
            return oApiResult;
        }

        /// <summary>
        /// 取得商餘額
        /// </summary>
        /// <param name="tpCustInfo"></param>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public override ApiResult<MerchantBalanceResponse> GetMerchantBalance(TpMerchantInfo tpMerchantInfo, Dictionary<string, string> inputs)
        {
            var currency = tpMerchantInfo.TpCurrencyId;
            var apiUrl = GetApiUrl(inputs);
            var adminName = GetAdminName(inputs, currency);
            var entityKey = GetEntityKey(inputs, currency);
            var pass = GetPass(inputs, currency);

            apiUrl = $"{apiUrl}/admin/info/adminname/{adminName}";

            var header = GetHeader(entityKey);
            var certificate = GetCertificate(currency, pass);

            var signUp = new
            {
                adminname = adminName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(signUp));

            var result = HttpPostWithX509(
              apiUrl,
              dicP,
              "application/x-www-form-urlencoded",
              header,
              certificate
          );

            var oApiResult = new ApiResult<MerchantBalanceResponse>();

            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = error.error ?? "";
                    oApiResult.RawErrorCode = error.errorcode.ToString() ?? "";
                }
                else
                {
                    var json = JsonConvert.DeserializeObject<TpMerchantBalanceResponse>(result);
                    oApiResult.Data = new MerchantBalanceResponse()
                    {
                        MerchantBalance = json.Result.MerchantBalance,
                        MerchantName = json.Result.MerchantName,
                        CurrencyName = currency
                    };
                    oApiResult.ErrorCode = ErrorCode.Success;
                    oApiResult.RawMessage = "";
                    oApiResult.RawErrorCode = "";
                }
            }
            else
            {
                oApiResult.ErrorCode = ErrorCode.Fail;
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
            }

            return oApiResult;
        }

        #region private method

        /// <summary>
        /// 取得憑證
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetCertificate(string currency, string pass)
        {
            var certificate = new Dictionary<string, string>
           {
               { "path", Path.Combine(storagePath, $"{currency}.p12") },
               { "password", pass }
           };
            return certificate;
        }

        /// <summary>
        /// 取得Http標頭
        /// </summary>
        /// <param name="entityKey"></param>
        /// <returns></returns>
        private static Dictionary<string, string> GetHeader(string entityKey)
        {
            var header = new Dictionary<string, string> { { "X_ENTITY_KEY", entityKey } };
            return header;
        }

        private static string GetPass(Dictionary<string, string> inputs, string currency)
        {
            return inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"]) ? inputs[$"Pass_{currency}"] : throw new Exception($"Pass_{currency}");
        }

        private static string GetEntityKey(Dictionary<string, string> inputs, string currency)
        {
            return inputs.ContainsKey($"XEntityKey_{currency}") && !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"]) ? inputs[$"XEntityKey_{currency}"] : throw new Exception($"XEntityKey_{currency}");
        }

        private static string GetAdminName(Dictionary<string, string> inputs, string currency)
        {
            return inputs.ContainsKey($"AdminName_{currency}") && !string.IsNullOrEmpty(inputs[$"AdminName_{currency}"]) ? inputs[$"AdminName_{currency}"] : throw new Exception($"AdminName_{currency}");
        }

        private static string GetApiUrl(Dictionary<string, string> inputs)
        {
            return inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
        }

        /// <summary>
        /// 檢查 Deposit 的 ErrorCode 是否要發送通知
        /// </summary>
        /// <param name="tpCustInfo"></param>
        /// <param name="Amount"></param>
        /// <param name="error"></param>
        private void CheckErrorCodeWithSendNotification(TpCustInfo tpCustInfo, decimal Amount, ApiErrorResponse error)
        {
            var adminDepositBalanceInsufficient = 97; //97 商戶餘額不足，玩家轉帳金額 > 商戶餘額
            if (error.errorcode == adminDepositBalanceInsufficient)
            {
                SandLesTalkMessage(tpCustInfo, Amount);
            }
        }

        /// <summary>
        /// 發送 LetsTalk 通知給客服
        /// </summary>
        /// <param name="config"></param>
        /// <param name="merchantBalanceResponse"></param>
        /// <returns></returns>
        private void SandLesTalkMessage(TpCustInfo tpCustInfo, decimal userTransferAmount)
        {
         
            var message = GetAlertMessage(tpCustInfo, userTransferAmount);

            var postData = new
            {
                GroupName = LetsTalkGroupName,
                Message = message
            };
            var parameters = JsonConvert.SerializeObject(postData);

            var httpResponse = HttpPostClient($"{CommonServiceHost}/Common/LetsTalk/SendMessage",
                parameters, "application/json");
        }

        private string GetAlertMessage(TpCustInfo tpCustInfo, decimal userTransferAmount)
        {
            var tpIdMapToSiteName = new Dictionary<int, string>()
            {
                { (int)SiteId.Nova88, "NOVA88" },
                { (int)SiteId.Goal123, "GOAL123" },
                { (int)SiteId.Bwing, "BWING" }
            };

            var message =
                $"* PlayTech User Transfer Balance Alert * \r\n" +
                $"* Time : {DateTime.UtcNow.AddHours(8):yyyy-MM-dd HH:mm:ss} * \r\n" +
                $"=======================\r\n" +
                $"User Transfer Balance more than the Merchant Balance \r\n" +
                $"Please Add Balance.\r\n" +
                $"=======================\r\n" +
                $"TpUserName：{tpCustInfo.TpUserName}\r\n" +
                $"TransferBalance：{userTransferAmount}\r\n" +
                $"Site：{tpIdMapToSiteName[tpCustInfo.SiteId]}\r\n" +
                $"TPName : PlayTech \r\n" +
                $"MerchantName : {tpIdMapToSiteName[tpCustInfo.SiteId]}{tpCustInfo.TpCurrencyId}\r\n" +
                $"Currency:{tpCustInfo.TpCurrencyId}";
            return message;
        }

        #endregion
    }
}
