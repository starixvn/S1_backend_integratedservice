using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;

namespace ThirdParty.PT
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly string storagePath = $@"{ContentRootPath}\Storage\PT";

        private readonly Dictionary<string, int> timeZoneList = new Dictionary<string, int>() {
            {"MYR",8},
            {"CNY",8},
            {"IDR",8},
            {"THB",7},
            {"VND",7},
            {"INR",5},
        };

        #region Param

        private class DepositParam
        {
            public string playername { get; set; }
            public decimal amount { get; set; }
            public string adminname { get; set; }
            public string externaltranid { get; set; }
        }

        private class WithdrawParam : DepositParam
        {
            public bool isForce { get; set; }
            public bool losebonus { get; set; }
        }

        #endregion

        #region Response

        private class ApiErrorResponse
        {
            public string? error { get; set; }
            public int errorcode { get; set; }
        }

        private class CreateMemberResponse
        {
            public CreateMemberResult result { get; set; }
        }

        private class CreateMemberResult
        {
            public string result { get; set; }
            public string playername { get; set; }
            public string password { get; set; }
        }

        private class ChangePasswordResponse
        {
            public ChangePasswordResult result { get; set; }
        }

        private class ChangePasswordResult
        {
            public string playername { get; set; }
            public string password { get; set; }
        }

        private class BalanceResponse
        {
            public BalanceResult result { get; set; }
        }

        private class BalanceResult
        {
            public string currencycode { get; set; }
            public decimal balance { get; set; }
            public decimal bonusbalance { get; set; }
            // public decimal rc_balance { get; set; }
            // public decimal current_bet { get; set; }
            public string playername { get; set; }
        }

        private class BrokenGamesResponse
        {
            public IList<BrokenGamesResult> result { get; set; }
        }

        public class BrokenGamesResult
        {
            public string PLAYERNAME { get; set; }
            public DateTime GAMEDATE { get; set; }
            public string CLIENTTYPE { get; set; }
            public string PLATFORM { get; set; }
            public string GAMENAME { get; set; }
            public string SHORTNAME { get; set; }
            public decimal BET { get; set; }
            public string REMOTEIP { get; set; }
            public string BROKENGAMETYPE { get; set; }
            public string BROKENGAMECODE { get; set; }
            public string STATUS { get; set; }
            public string FINISHEDGAMECODE { get; set; }
            public DateTime FINISHEDGAMEDATE { get; set; }

        }

        private class TransferResponse
        {
            public TransResult result { get; set; }
        }

        private class TransResult
        {
            public decimal amount { get; set; }
            public string currentplayerbalance { get; set; }
            public string executiontime { get; set; }
            public string externaltransactionid { get; set; }
            public string instantcashtype { get; set; }
            public string kiosktransactionid { get; set; }
            public DateTime kiosktransactiontime { get; set; }
            public string ptinternaltransactionid { get; set; }
            public string result { get; set; }
        }

        private class CheckTransferResponse
        {
            public CheckTransResult result { get; set; }
        }

        private class CheckTransResult
        {
            public string status { get; set; }
            public string externaltransactionid { get; set; }
            public string executiontime { get; set; }
        }

        #endregion

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            try
            {
                string currency = tpCustInfo.TpCurrencyId;
                string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"])
                    ? inputs["ApiUrl"]
                    : throw new Exception("ApiUrl");
                string EntityKey =
                    inputs.ContainsKey($"XEntityKey_{currency}") &&
                    !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"])
                        ? inputs[$"XEntityKey_{currency}"]
                        : throw new Exception($"XEntityKey_{currency}");
                string Pass =
                    inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"])
                        ? inputs[$"Pass_{currency}"]
                        : throw new Exception($"Pass_{currency}");
                ApiUrl = $"{ApiUrl}/player/checktransaction";

                Dictionary<string, string> header = new Dictionary<string, string>();
                header.Add("X_ENTITY_KEY", EntityKey);

                Dictionary<string, string> certificate = new Dictionary<string, string>();
                certificate.Add("path", $@"{storagePath}\{currency}.p12");
                certificate.Add("password", Pass);

                var CheckTransfer = new
                {
                    externaltransactionid = TransID,
                };

                var dicP = FromJson<Dictionary<string, string>>(ToJson(CheckTransfer));
                var sParam = string.Join("&",
                    dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

                var result = HttpPostWithX509(
                    ApiUrl,
                    sParam,
                    "application/x-www-form-urlencoded",
                    header,
                    certificate
                );

                ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
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

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string AdminName = inputs.ContainsKey($"AdminName_{currency}") && !string.IsNullOrEmpty(inputs[$"AdminName_{currency}"]) ? inputs[$"AdminName_{currency}"] : throw new Exception($"AdminName_{currency}");
            string EntityKey = inputs.ContainsKey($"XEntityKey_{currency}") && !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"]) ? inputs[$"XEntityKey_{currency}"] : throw new Exception($"XEntityKey_{currency}");
            string Pass = inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"]) ? inputs[$"Pass_{currency}"] : throw new Exception($"Pass_{currency}");
            string Prefix = inputs.ContainsKey("Prefix") && !string.IsNullOrEmpty(inputs["Prefix"]) ? inputs["Prefix"] : throw new Exception("Prefix");
            ApiUrl = $"{ApiUrl}/player/create";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("X_ENTITY_KEY", EntityKey);

            Dictionary<string, string> certificate = new Dictionary<string, string>();
            certificate.Add("path", $@"{storagePath}\{currency}.p12");
            certificate.Add("password", Pass);

            string playerName = $"{Prefix}_{tpCustInfo.TpUserName}";

            var SignUp = new
            {
                playername = playerName.ToUpper(),
                adminname = AdminName,
                password = tpCustInfo.TpPwd,
                custom02 = Prefix
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(SignUp));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPostWithX509(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            ApiResult oApiResult = new ApiResult();
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

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string EntityKey = inputs.ContainsKey($"XEntityKey_{currency}") && !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"]) ? inputs[$"XEntityKey_{currency}"] : throw new Exception($"XEntityKey_{currency}");
            string Pass = inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"]) ? inputs[$"Pass_{currency}"] : throw new Exception($"Pass_{currency}");
            ApiUrl = $"{ApiUrl}/player/balance";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("X_ENTITY_KEY", EntityKey);

            Dictionary<string, string> certificate = new Dictionary<string, string>();
            certificate.Add("path", $@"{storagePath}\{currency}.p12");
            certificate.Add("password", Pass);

            var Balance = new
            {
                playername = tpCustInfo.TpUserName
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Balance));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPostWithX509(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
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
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            string client = "";
            if (gameId == "0")
            {
                gameId = "";
                // live casino
                if (appType == AppType.Desktop)
                {
                    client = "live_desk";
                }
                else
                {
                    client = "live_mob";
                }
            }
            else
            {
                // slot
                if (appType == AppType.Desktop)
                {
                    client = "ngm_desktop";
                }
                else
                {
                    client = "ngm_mobile";
                }
            }

            if (fun.Equals("0", StringComparison.OrdinalIgnoreCase))
            {
                // change password (error:2) - will also clean lock minutes
                ChangePassword(tpCustInfo, inputs);
            }

            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            oApiResult.Message = JsonConvert.SerializeObject(new
            {
                account = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "guest" : tpCustInfo.TpUserName,
                password = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "guest" : tpCustInfo.TpPwd,
                game = gameId,
                lang = TpLangId,
                client = client,
                mode = fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? "offline" : "real"
            });
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string AdminName = inputs.ContainsKey($"AdminName_{currency}") && !string.IsNullOrEmpty(inputs[$"AdminName_{currency}"]) ? inputs[$"AdminName_{currency}"] : throw new Exception($"AdminName_{currency}");
            string EntityKey = inputs.ContainsKey($"XEntityKey_{currency}") && !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"]) ? inputs[$"XEntityKey_{currency}"] : throw new Exception($"XEntityKey_{currency}");
            string Pass = inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"]) ? inputs[$"Pass_{currency}"] : throw new Exception($"Pass_{currency}");

            string TransUrl = $"{ApiUrl}/player/deposit";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("X_ENTITY_KEY", EntityKey);

            Dictionary<string, string> certificate = new Dictionary<string, string>();
            certificate.Add("path", $@"{storagePath}\{currency}.p12");
            certificate.Add("password", Pass);

            var Transfer = new DepositParam
            {
                playername = tpCustInfo.TpUserName,
                amount = Math.Floor(Amount * 100) / 100,
                adminname = AdminName,
                externaltranid = TransID
            };

            if (direction == Direction.ToMain)
            {
                TransUrl = $"{ApiUrl}/player/withdraw";
                Transfer = new WithdrawParam
                {
                    playername = tpCustInfo.TpUserName,
                    amount = Math.Floor(Amount * 100) / 100,
                    adminname = AdminName,
                    isForce = false,
                    externaltranid = TransID,
                    losebonus = false
                };
            }

            var dicP = FromJson<Dictionary<string, string>>(ToJson(Transfer));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPostWithX509(
                TransUrl,
                sParam,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            ApiResult<TransferResult> apiResult = new ApiResult<TransferResult>();
            if (IsJSON(result))
            {
                var error = JsonConvert.DeserializeObject<ApiErrorResponse>(result);
                if (error.error != null)
                {
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

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            try
            {
                int timeSpan = timeZoneList[tpCustInfo.TpCurrencyId];

                var apiUrl = inputs.GetValueOrDefault("ApiUrl") ?? throw new Exception();
                var entityKey = inputs.GetValueOrDefault($"XEntityKey_{tpCustInfo.TpCurrencyId}") ?? throw new Exception();
                var pass = inputs.GetValueOrDefault($"Pass_{tpCustInfo.TpCurrencyId}") ?? throw new Exception();
  
                apiUrl = $"{apiUrl}/player/games";
                var header = new Dictionary<string, string>
                {
                    { "X_ENTITY_KEY", entityKey }
                };
                var certificate = new Dictionary<string, string>
                {
                    { "path", $@"{storagePath}\{tpCustInfo.TpCurrencyId}.p12" },
                    { "password", pass }
                };

                var wagerDetail = TpBetRecordProvider.WagerDbQueryFirst("Service_TpWager_PT_Detail_Get",
                   new { getBetDetail.TransId, GameType = getBetDetail.TpGameTypeOption.Type });
                var sParam = new Dictionary<string, string>
                {
                    { "playername", wagerDetail.GetValueOrDefault("PLAYERNAME").ToString()},
                    { "gametype", wagerDetail.GetValueOrDefault("SHORTNAME").ToString() },
                    { "startdate", DateTime.Parse( wagerDetail.GetValueOrDefault("GAMEDATE").ToString()).AddHours(timeSpan).ToString("yyyy-MM-dd HH:mm:ss") },
                    { "enddate", DateTime.Parse( wagerDetail.GetValueOrDefault("GAMEDATE").ToString()).AddHours(timeSpan).ToString("yyyy-MM-dd HH:mm:ss") },
                    { "currency", wagerDetail.GetValueOrDefault("CURRENCYCODE").ToString() }
                };

                var json = HttpPostWithX509(
                    apiUrl,
                    string.Join("&", sParam.Select(kp => $"{kp.Key}={kp.Value}")),
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

        private ApiResult ChangePassword(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            string EntityKey = inputs.ContainsKey($"XEntityKey_{currency}") && !string.IsNullOrEmpty(inputs[$"XEntityKey_{currency}"]) ? inputs[$"XEntityKey_{currency}"] : throw new Exception($"XEntityKey_{currency}");
            string Pass = inputs.ContainsKey($"Pass_{currency}") && !string.IsNullOrEmpty(inputs[$"Pass_{currency}"]) ? inputs[$"Pass_{currency}"] : throw new Exception($"Pass_{currency}");
            ApiUrl = $"{ApiUrl}/player/changepassword";

            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("X_ENTITY_KEY", EntityKey);

            Dictionary<string, string> certificate = new Dictionary<string, string>();
            certificate.Add("path", $@"{storagePath}\{currency}.p12");
            certificate.Add("password", Pass);

            var ChangePassword = new
            {
                playername = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd,
            };

            var dicP = FromJson<Dictionary<string, string>>(ToJson(ChangePassword));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            var result = HttpPostWithX509(
                ApiUrl,
                sParam,
                "application/x-www-form-urlencoded",
                header,
                certificate
            );

            ApiResult oApiResult = new ApiResult();
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


        public class DetailResult
        {
            public List<Result> result { get; set; }
        }

        public class Result
        {
            public string URL { get; set; }
        }

    }
}
