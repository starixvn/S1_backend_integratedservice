using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class BTIService : IThirdPartyService
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public BTIService(IServiceProvider provider)
        {
            ThirdParty = Tp.BTI;
            GameType = GameType.BTI_Sports;
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
        }

        public BTIStatusObj Staus(string token)
        {
            try
            {
                var (authToken, sessionToken, tpUserName) = GetToken(token);

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpUserName);
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

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpUserName);
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
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpUserName);
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
                    { "error_message", ex.Message },
                });
            }
            catch (RestrictedCustomerException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message", ex.Message },
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
               
                if (btiResult.SPResult == -1)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-1" },
                        { "error_message", "general error." },
                    });
                }

                if (btiResult.SPResult == 0)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AfterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                if (btiResult.SetBalanceResult < 1 || btiResult.AfterAmount < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-4" },
                        { "error_message", "InsufficientFunds" },
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() },
                    { "trx_id", btiResult.Trx_id.ToString()},
                });
            }
            catch (CustomerNotFoundException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-2" },
                    { "error_message", ex.Message },
                });
            }
            catch (RestrictedCustomerException ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message", ex.Message },
                });
            }
        }

        public async Task<string> DebitReserveAsync(BTIRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await bettingService.DebitReserve_BTIAsync(tpCustInfo, new BTIDebitRequest(request));
                btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "trx_id", btiResult.Trx_id.ToString()},
                    { "balance", btiResult.AfterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                    { "balance", "0" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                });
            }
        }

        public async Task<string> CancelReserveAsync(BTIRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.CancelReserve_BTIAsync(tpCustInfo, request);
                if (btiResult.SPResult < 1)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", btiResult.SPResult == 0 ?
                        $"reserve_id:{request.reserve_id} is cancel done." : $"reserve_id:{request.reserve_id} was not found." },
                        { "balance", btiResult.AfterAmount.ToString() },
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                    { "balance", "0" },
                });
            }
        }

        public async Task<string> CommitReserve(BTIRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.CommitReserve_BTIAsync(tpCustInfo, request);

                if (btiResult.SPResult < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", $"reserve_id:{request.reserve_id} was not found." },
                        { "balance", "0" },
                        { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    });
                }

                if (btiResult.ReturnAmount <= 0 && btiResult.AfterAmount == 0)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AfterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() },
                    { "trx_id", btiResult.Trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                    { "balance", "0" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                });
            }
        }

        public async Task<string> Credit(BTIRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.Credit_BTIAsync(tpCustInfo, new BTICreditRequest(request));

                if (btiResult.SPResult == 0)
                {
                    btiResult.AfterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AfterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                btiResult.AfterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AfterAmount.ToString() },
                    { "trx_id", btiResult.Trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                    { "balance", "0" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                });
            }
        }

        private (string authToken, string sessionToken, string tpUserName) GetToken(string auth_token)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(auth_token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
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

        private string GetCountryCode(string TpCurrencyId)
        {
            return TpCurrencyId switch
            {
                "VND" => "VN",
                _ => throw new RestrictedCustomerException($"CurrencyId:{TpCurrencyId} not supported.")
            };

        }

    }
}
