using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TCGBLL : ITCGBLL, IThirdPartyBLL
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        private readonly ThirdPartySetting thirdPartySetting;
        private readonly Dictionary<string, Func<TCGRequest, TCGConfig, Task<dynamic>>> _requestHandler;
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public TCGBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.TCG;
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
            thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);
            _requestHandler = new Dictionary<string, Func<TCGRequest, TCGConfig, Task<dynamic>>>()
            {
                { TCGApiMethod.GetBalance, GetBalance},
                { TCGApiMethod.Debit, Debit},
                { TCGApiMethod.Credit, Credit},
                { TCGApiMethod.Reserve, Reserve},
                { TCGApiMethod.CancelReserve, CancelReserve},
                { TCGApiMethod.ConfirmReserve, ConfirmReserve},
                { TCGApiMethod.CheckTransactionStatus, CheckTransactionStatus},
            };
        }

        public async Task<dynamic> SeamlessWallet(string merchantCode, string @params, string sign)
        {
            try
            {
                var tcgConfig = new TCGConfig(thirdPartySetting.TpSetting, merchantCode);
                var jsonString = DesDecrypt(@params, tcgConfig.DesKey);
                var desEncrypt = DesEncrypt(jsonString, tcgConfig.DesKey);
                var checkSign = SHA256($"{desEncrypt}{tcgConfig.SignKey}");
                if (sign != checkSign)
                {
                    throw new TCGException(TCGErrorCode.InvalidSignature, $"Invalid signature | RequestSign is:{sign} | CheckSign is {checkSign}");
                }

                var request = JsonConvert.DeserializeObject<TCGRequest>(jsonString);
                return await _requestHandler[request.method].Invoke(request, tcgConfig);
            }
            catch (TCGException ex)
            {
                return new TCGBaseResult()
                {
                    status = ex.ErrorCode,
                    error_desc = ex.Message,
                };
            }
        }

        private async Task<dynamic> GetBalance(TCGRequest request, TCGConfig tcgConfig)
        {
            var tpUserName = request.username;
            var (tpCustInfo, tpCurrency) = GetTPInfo(tpUserName);
            var gameType = GetGameType(request.product_type);
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)gameType);
            return new
            {
                status = "0",
                error_desc = "Success",
                balance = tpCustBalance.Balance.ToString(),
            };
        }

        private async Task<dynamic> Debit(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        private async Task<dynamic> Credit(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        private async Task<dynamic> Reserve(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        private async Task<dynamic> CancelReserve(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        private async Task<dynamic> ConfirmReserve(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        private async Task<dynamic> CheckTransactionStatus(TCGRequest request, TCGConfig tcgConfig)
        {
            return new
            {
                status = "0",
                error_desc = "Success",
            };
        }

        
        /// <summary>
        /// DES加密
        /// </summary>
        private string DesEncrypt(string input, string key)
        {
            var toEncryptArray = Encoding.UTF8.GetBytes(input);
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using var cTransform = des.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        private string DesDecrypt(string pToDecrypt, string key)
        {
            var dataByteArray = Convert.FromBase64String(pToDecrypt);
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(dataByteArray, 0, dataByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }

        private GameType GetGameType(int productType)
        {
            return productType switch
            {
                384 => GameType.TCG_SEA_Lottery,
                420 => GameType.TCG_VN_Lottery,
                _ => throw new TCGException(TCGErrorCode.MerchantNotAllowedProductType, $"product_type:{productType} not supported.")
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new TCGException(TCGErrorCode.UserDoesNotExists, $"Customer:{tpUserName} not found.");
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new TCGException(TCGErrorCode.UnSupportedCurrency, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }
        
        
        #region BTI

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

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "cust_id", tpUserName},
                    { "balance", tpCustBalance.Balance.ToString()},
                    { "cust_login", tpUserName },
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



        private string ReturnBTI(Dictionary<string, string> dic)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dic)
            {
                sb.AppendLine($"{item.Key}={item.Value}");
            }
            return sb.ToString();
        }


        #endregion
    }
}
