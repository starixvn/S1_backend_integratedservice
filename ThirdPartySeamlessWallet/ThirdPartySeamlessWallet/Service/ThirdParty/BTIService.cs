using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.ThirdParty.Interface;

namespace ThirdPartySeamlessWallet.Service.ThirdParty
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

        public string ValidateToken(string auth_token)
        {
            try
            {
                // get token
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(auth_token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var authToken = sessionKeyArr[0]; /* 1 Time*/
                var sessionToken = sessionKeyArr[1];/* Long Time*/

                // check token
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", $"authToken invalid." },
                    });
                }
                if (tpCustInfoAuth.TpCurrencyId == null)
                {
                    throw new NotSupportedException();
                }

                // check tp status and sessionToken
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", "tpCheck fail." },
                    });
                }
                if (tpCheck.Locked)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-6" },
                        { "error_message", "Restricted customer." },
                    });
                }
                if (tpCheck.Token != sessionToken)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", "sessionToken expire." },
                    });
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfoAuth.TpUserName, (int)GameType);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "cust_id", tpCustInfoAuth.TpUserName},
                    { "balance", tpCustBalance.Balance.ToString()},
                    { "cust_login", tpCustInfoAuth.TpUserName },
                    { "city", "" },
                    { "country", GetCountryCode(tpCustInfoAuth.TpCurrencyId)},
                    { "currency_code", tpCustBalance.TpCurrencyId},
                });
            }
            catch (NotSupportedException)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message", "Restricted customer." },
                });
            }
        }

        public async Task<string> ReserveAsync(BTIRequestObj request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;

            var bTIResult = await bettingService.Reserve_BTIAsync(tpCustInfo, request);

            if (bTIResult.SPResult == 0)
            {
                bTIResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", bTIResult.AffterAmount.ToString() },
                    { "trx_id", bTIResult.Trx_id.ToString()},
                });
            }

            if (bTIResult.SetBalanceResult < 1 || bTIResult.AffterAmount < 0)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-4" },
                    { "error_message", "InsufficientFunds" },
                });
            }

            bTIResult.AffterAmount /= tpCurrency.ExchangeRate;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", "0" },
                { "error_message", "OK" },
                { "balance", bTIResult.AffterAmount.ToString() },
                { "trx_id", bTIResult.Trx_id.ToString()},
            });
        }

        public async Task<string> DebitReserveAsync(BTIRequestObj request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;


            var bTIResult = await bettingService.DebitReserve_BTIAsync(tpCustInfo, new BTiDebitRequest(request));
            bTIResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", "0" },
                { "error_message", "OK" },
                { "trx_id", bTIResult.Trx_id.ToString()},
                { "balance", bTIResult.AffterAmount.ToString() },
            });
        }

        public async Task<string> CancelReserveAsync(BTIRequestObj request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;

            var bTIResult = await settledService.CancelReserve_BTIAsync(tpCustInfo, request);
            if (bTIResult.SPResult < 1)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "Reserve was not found." },
                });
            }

            bTIResult.AffterAmount /= tpCurrency.ExchangeRate;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", "0" },
                { "error_message", "OK" },
                { "balance", bTIResult.AffterAmount.ToString() },
            });
        }

        public async Task<string> CommitReserve(BTIRequestObj request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;

            var bTIResult = await settledService.CommitReserve_BTIAsync(tpCustInfo, request);

            if (bTIResult.SPResult < 1)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "Reserve was not found." },
                });
            }

            bTIResult.AffterAmount /= tpCurrency.ExchangeRate;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", "0" },
                { "error_message", "OK" },
                { "balance", bTIResult.AffterAmount.ToString() },
                { "trx_id", bTIResult.Trx_id.ToString()},
            });
        }

        public async Task<string> Credit(BTIRequestObj request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.cust_id);
            request.amount *= tpCurrency.ExchangeRate;

            var bTIResult = await settledService.Credit_BTIAsync(tpCustInfo, new BTiCreditRequest(request));

            if (bTIResult.SPResult == 0)
            {
                bTIResult.AffterAmount = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType).Balance;
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", bTIResult.AffterAmount.ToString() },
                    { "trx_id", bTIResult.Trx_id.ToString()},
                });
            }

            bTIResult.AffterAmount /= tpCurrency.ExchangeRate;
            return ReturnBTI(new Dictionary<string, string>
            {
                { "error_code", "0" },
                { "error_message", "OK" },         
                { "balance", bTIResult.AffterAmount.ToString() },
                { "trx_id", bTIResult.Trx_id.ToString()},
            });
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new NotSupportedException();
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
                _ => throw new NotSupportedException()
            };

        }

    }
}
