using System.Text;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

using static ThirdPartySeamlessWallet.Controllers.APController;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class BTIBLL : IThirdPartyBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        private readonly ILogger<BTIBLL> _logger;
        private readonly IHostEnvironment _environment;
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public BTIBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.BTI;
            GameType = GameType.BTI_Sports;
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<BTIBLL>>();
            _environment = provider.GetRequiredService<IHostEnvironment>();
        }

        public BTIStatusObj Staus(string token)
        {
            try
            {
                var (authToken, sessionToken, tpUserName) = GetToken(token);

                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
                if (tpCheck == null || tpCheck.Locked || tpCheck.Token != sessionToken)
                {
                    return new BTIStatusObj(token);
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpUserName, (int)GameType);

                return new BTIStatusObj()
                {
                    token = token,
                    uid = Guid.NewGuid().ToString(),
                    status = "real",
                    message = "Success",
                    balance = $"{tpCustBalance.Balance}"
                };
            }
            catch (Exception)
            {
                return new BTIStatusObj(token);
            }
        }

        public BTIRefreshObj Refresh(string token)
        {
            try
            {
                var (authToken, sessionToken, tpUserName) = GetToken(token);

                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
                if (tpCheck == null || tpCheck.Locked || tpCheck.Token != sessionToken)
                {
                    return new BTIRefreshObj(false);
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpUserName, (int)GameType);

                return new BTIRefreshObj()
                {
                    status = "success",
                    message = "Success",
                    balance = $"{tpCustBalance.Balance}"
                };
            }
            catch (Exception)
            {
                return new BTIRefreshObj(false);
            }
        }

        public string ValidateToken(string auth_token)
        {
            try
            {
                var (authToken, sessionToken, tpUserName) = GetToken(auth_token);

                // check tp status and sessionToken
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
                if (tpCheck == null)
                {
                    throw new InvalidTokenException("tpCheck fail.");
                }
                if (tpCheck.Locked)
                {
                    throw new RestrictedCustomerException("tpuser is locked.");
                }
                if (tpCheck.Token != sessionToken)
                {
                    throw new InvalidTokenException("sessionToken expire.");
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpUserName, (int)GameType);
                var countryCode = GetCountryCode(tpCustBalance.TpCurrencyId);

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "cust_id", tpUserName},
                    { "balance", tpCustBalance.Balance.ToString()},
                    { "cust_login", tpUserName },
                    { "city", countryCode },
                    { "country", countryCode},
                    { "currency_code", tpCustBalance.TpCurrencyId},
                });
            }
            catch (InvalidTokenException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-3" },
                    { "error_message", $"Fail:{ex.Message}" },
                });
            }
            catch (RestrictedCustomerException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message",$"Fail:{ex.Message}" },
                });
            }
        }

        public async Task<string> ReserveAsync(BTIRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await bettingService.Reserve_BTIAsync(tpCustInfo, request);
                var beforeBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                if (btiResult.SPResult == -1)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-1" },
                        { "error_message", "Fail:general error." },
                        { "trx_id",btiResult.Trx_id.ToString()},
                        { "balance", beforeBalance.ToString() }
                    });
                }

                if (btiResult.SPResult == 0)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "trx_id", btiResult.Trx_id.ToString()},
                          { "balance", beforeBalance.ToString() }
                    });
                }

                if (btiResult.SetBalanceResult < 1 || btiResult.AfterAmount < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-4" },
                        { "error_message", "Fail:InsufficientFunds" },
                        { "trx_id", btiResult.Trx_id.ToString()},
                         { "balance", beforeBalance.ToString() }
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "trx_id", btiResult.Trx_id.ToString()},
                    { "balance", btiResult.AfterAmount.ToString() }
                });
            }
            catch (CustomerNotFoundException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-2" },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
            catch (RestrictedCustomerException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
        }

        public async Task<string> DebitReserveAsync(BTIRequestObj request)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await bettingService.DebitReserve_BTIAsync(tpCustInfo, new BTIDebitRequest(request));
                btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                if (btiResult.SPResult < 1)
                {
                    var errorMessage = string.Empty;
                    switch (btiResult.SPResult)
                    {
                        case 0:
                            errorMessage = $"reserve_id:{request.reserve_id} is cancel/commit done.";
                            break;
                        case -2:
                            errorMessage = $"reserve_id:{request.reserve_id} is debit done.";
                            break;
                        default: //-1:need BTI resend request
                            if (!_environment.IsDevelopment()) //PRD
                                errorCode = "-1";
                            errorMessage = $"reserve_id:{request.reserve_id} was not found.";
                            break;
                    }
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", errorCode },
                        { "error_message", $"Fail:{errorMessage}" },
                        { "trx_id", btiResult.Trx_id.ToString()},
                        { "balance", btiResult.AfterAmount.ToString() }
                    });
                }
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", "OK" },
                    { "trx_id", btiResult.Trx_id.ToString()},
                    { "balance", btiResult.AfterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception：{ex.Message}");

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:Exception happened." },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
        }

        public async Task<string> CancelReserveAsync(BTIRequestObj request)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.CancelReserve_BTIAsync(tpCustInfo, request);
                if (btiResult.SPResult < 1)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    var errorMessage = string.Empty;
                    switch (btiResult.SPResult)
                    {
                        case 0:
                            errorMessage = $"reserve_id:{request.reserve_id} is cancel/commit done.";
                            break;
                        case -2:
                            errorMessage = $"reserve_id:{request.reserve_id} is debit done.";
                            break;
                        default:
                            errorMessage = $"reserve_id:{request.reserve_id} was not found.";
                            break;
                    }
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code",errorCode },
                        { "error_message",  $"Fail:{errorMessage}" },
                        { "balance", btiResult.AfterAmount.ToString() },
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception：{ex.Message}");

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", "Fail:Exception happened." },
                    { "balance", "0" },
                });
            }
        }

        public async Task<string> CommitReserve(BTIRequestObj request)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.CommitReserve_BTIAsync(tpCustInfo, request);
                var beforeBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                if (btiResult.SPResult < 1)
                {
                    var errorMessage = string.Empty;
                    switch (btiResult.SPResult)
                    {
                        case 0:
                            errorMessage = $"reserve_id:{request.reserve_id} is cancel/commit done.";
                            break;
                        default: //-1:need BTI resend request
                            if (!_environment.IsDevelopment()) //PRD
                                errorCode = "-1";
                            errorMessage = $"reserve_id:{request.reserve_id} was not found.";
                            break;
                    }

                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", errorCode },
                        { "error_message", $"Fail:{errorMessage}" },
                        { "balance", beforeBalance.ToString() }
                    });
                }

                if (btiResult.ReturnAmount <= 0 && btiResult.AfterAmount == 0)
                {
                    _logger.LogInformation($"reserve_id:{request.reserve_id} and purchase_id:{request.purchase_id} ReturnAmount <=0 And AfterAmount=0.");
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", errorCode },
                        { "error_message", "OK" },
                        { "balance", beforeBalance.ToString() }
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception：{ex.Message}");

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", "Fail:Exception happened." },
                    { "balance", "0" }
                });
            }
        }

        public async Task<string> Credit(BTIRequestObj request)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;

            var creditRequest = new BTICreditRequest(request);
            var btiResult = await settledService.Credit_BTIAsync(tpCustInfo, creditRequest);

            if (btiResult.SPResult < 1)
            {
                btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                var errorMessage = string.Empty;
                switch (btiResult.SPResult)
                {
                    case 0:
                        errorMessage = $"reserve_id:{creditRequest.reserve_id} is reverse/cancel done.";
                        break;
                    case -2:
                        errorMessage = $"reserve_id:{creditRequest.reserve_id} is credit done.";
                        break;
                    case -3://:need BTI resend request
                        if (!_environment.IsDevelopment()) //PRD
                            errorCode = "-3";
                        errorMessage = $"reserve_id:{creditRequest.reserve_id} is not exist PurchaseBetID:{creditRequest.PurchaseID}.";
                        break;
                    case -4://:need BTI resend request
                        if (!_environment.IsDevelopment()) //PRD
                            errorCode = "-4";
                        errorMessage = $"reserve_id:{creditRequest.reserve_id} PurchaseBetID not same with PurchaseID:{creditRequest.PurchaseID}.";
                        break;
                    default: //-1:need BTI resend request
                        if (!_environment.IsDevelopment()) //PRD
                            errorCode = "-1";
                        errorMessage = $"reserve_id:{creditRequest.reserve_id} was not found.";
                        break;
                }

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{errorMessage}" },
                    { "trx_id", btiResult.Trx_id.ToString()},
                    { "balance", btiResult.AfterAmount.ToString() }
                });
            }

            btiResult.AfterAmount /= tpCurrency.ExchangeRate;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", errorCode },
                { "error_message", "OK" },
                { "trx_id", btiResult.Trx_id.ToString()},
                { "balance", btiResult.AfterAmount.ToString() }
            });
        }

        private (string authToken, string sessionToken, string tpUserName) GetToken(string auth_token)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(auth_token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                _logger.LogInformation($"{sessionKeyArr[2]}");
                return (sessionKeyArr[0], sessionKeyArr[1], sessionKeyArr[2]);
            }
            catch
            {
                throw new InvalidTokenException($"auth_token:{auth_token} is invalid token.");
            }
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new CustomerNotFoundException($"Customer:{tpUserName} not found.");
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new RestrictedCustomerException($"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            _logger.LogInformation($"{tpCustInfo.TpUserName}");
            return (tpCustInfo, tpCurrency);
        }

        private string ReturnBTI(Dictionary<string, string> dic)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dic)
            {
                sb.AppendLine($"{item.Key}={item.Value}");
            }
            return sb.ToString();
        }

        private string GetCountryCode(string tpCurrencyId)
        {
            var tpCountryCode = tpCustInfoBLL.GetTpCurrencyCodeByCurrencyId((int)ThirdParty, tpCurrencyId);
            if (tpCountryCode == null)
            {
                throw new RestrictedCustomerException($"CurrencyId:{tpCurrencyId} not supported.");
            }
            return tpCountryCode.CountryCode;
        }

    }
}
