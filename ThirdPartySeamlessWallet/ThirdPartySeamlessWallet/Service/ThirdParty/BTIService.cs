using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.ThirdParty.Interface;

namespace ThirdPartySeamlessWallet.Service.ThirdParty
{
    public class BtiService : IThirdPartyService
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public BtiService(IServiceProvider provider)
        {
            ThirdParty = Tp.BTI;
            GameType = GameType.BTI_Sports;
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
        }

        public BtiStatusObj Staus(string token)
        {
            try
            {
                var (authToken, sessionToken) = GetToken(token);
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return new BtiStatusObj(token);
                }

                if (tpCustInfoAuth.TpCurrencyId == null)
                {
                    return new BtiStatusObj(token);
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpCustInfoAuth.TpUserName);
                if (tpCheck == null || tpCheck.Locked || tpCheck.Token != sessionToken)
                {
                    return new BtiStatusObj(token);
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfoAuth.TpUserName, (int)GameType);
                return new BtiStatusObj()
                {
                    token = token,
                    uid = Guid.NewGuid().ToString(),
                    status = "real",
                    message = "Success",
                    balance = $"{tpCustBalance.Balance} {tpCustBalance.TpCurrencyId}"
                };
            }
            catch (Exception)
            {
                return new BtiStatusObj(token);
            }
        }

        public BtiRefreshObj Refresh(string token)
        {
            try
            {
                var (authToken, sessionToken) = GetToken(token);
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return new BtiRefreshObj(false);
                }

                if (tpCustInfoAuth.TpCurrencyId == null)
                {
                    return new BtiRefreshObj(false);
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpCustInfoAuth.TpUserName);
                if (tpCheck == null || tpCheck.Locked || tpCheck.Token != sessionToken)
                {
                    return new BtiRefreshObj(false);
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfoAuth.TpUserName, (int)GameType);
                return new BtiRefreshObj()
                {
                    status = "success",
                    message = "Success",
                    balance = $"{tpCustBalance.Balance} {tpCustBalance.TpCurrencyId}"
                };
            }
            catch (Exception)
            {
                return new BtiRefreshObj(false);
            }
        }

        public string ValidateToken(string auth_token)
        {
            try
            {
                var (authToken, sessionToken) = GetToken(auth_token);

                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    throw new InvalidTokenException("authToken invalid.");
                }
                if (tpCustInfoAuth.TpCurrencyId == null)
                {
                    throw new RestrictedCustomerException("Restricted customer.");
                }

                // check tp status and sessionToken
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpCustInfoAuth.TpUserName);
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

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfoAuth.TpUserName, (int)GameType);
                var countryCode = GetCountryCode(tpCustInfoAuth.TpCurrencyId);

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "cust_id", tpCustInfoAuth.TpUserName},
                    { "balance", tpCustBalance.Balance.ToString()},
                    { "cust_login", tpCustInfoAuth.TpUserName },
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

        public async Task<string> ReserveAsync(BtiRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await bettingService.Reserve_BTIAsync(tpCustInfo, request);

                if (btiResult.SPResult == 0)
                {
                    btiResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AffterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                if (btiResult.SetBalanceResult < 1 || btiResult.AffterAmount < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-4" },
                        { "error_message", "InsufficientFunds" },
                    });
                }

                btiResult.AffterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AffterAmount.ToString() },
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

        public async Task<string> DebitReserveAsync(BtiRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await bettingService.DebitReserve_BTIAsync(tpCustInfo, new BtiDebitRequest(request));
                btiResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "trx_id", btiResult.Trx_id.ToString()},
                    { "balance", btiResult.AffterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                });
            }
        }

        public async Task<string> CancelReserveAsync(BtiRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.CancelReserve_BTIAsync(tpCustInfo, request);
                if (btiResult.SPResult < 1)
                {
                    btiResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", btiResult.SPResult == 0 ?
                        $"reserve_id:{request.reserve_id} is cancel done." : $"reserve_id:{request.reserve_id} was not found." },
                        { "balance", btiResult.AffterAmount.ToString() },
                    });
                }

                btiResult.AffterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AffterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                });
            }
        }

        public async Task<string> CommitReserve(BtiRequestObj request)
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
                    });
                }

                if (btiResult.RetrunAmount <= 0 && btiResult.AffterAmount == 0)
                {
                    btiResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AffterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                btiResult.AffterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AffterAmount.ToString() },
                    { "trx_id", btiResult.Trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                });
            }
        }

        public async Task<string> Credit(BtiRequestObj request)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
                request.amount *= tpCurrency.ExchangeRate;

                var btiResult = await settledService.Credit_BTIAsync(tpCustInfo, new BtiCreditRequest(request));

                if (btiResult.SPResult == 0)
                {
                    btiResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "OK" },
                        { "balance", btiResult.AffterAmount.ToString() },
                        { "trx_id", btiResult.Trx_id.ToString()},
                    });
                }

                btiResult.AffterAmount /= tpCurrency.ExchangeRate;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", btiResult.AffterAmount.ToString() },
                    { "trx_id", btiResult.Trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", ex.Message },
                });
            }
        }

        private (string authToken, string sessionToken) GetToken(string auth_token)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(auth_token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                return (sessionKeyArr[0], sessionKeyArr[1]);
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
