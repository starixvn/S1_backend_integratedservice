using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class TCGBLL : ITCGBLL, IThirdPartyBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        private readonly ILogger<TCGBLL> _logger;
        private readonly Dictionary<string, Func<TCGRequest, TCGConfig, Task<TCGReturnObj>>> _requestHandler;
        public readonly Dictionary<string, string> DefaultConfig; // SiteConfig的SiteId=0
        private readonly Dictionary<string, Dictionary<string, string>> _siteNameConfigs;

        private int _processorCount { get; set; }

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public TCGBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.TCG;
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
            _requestHandler = new Dictionary<string, Func<TCGRequest, TCGConfig, Task<TCGReturnObj>>>()
            {
                { TCGApiMethod.GetBalance, GetBalance},
                { TCGApiMethod.Debit, Transaction},
                { TCGApiMethod.Credit, Transaction},
                { TCGApiMethod.Reserve, Transaction},
                { TCGApiMethod.CancelReserve, Reservation},
                { TCGApiMethod.ConfirmReserve, Reservation},
                { TCGApiMethod.CheckTransactionStatus, CheckTransactionStatus},
            };
            _logger = provider.GetRequiredService<ILogger<TCGBLL>>();
            LogContext.PushProperty("TP", ThirdParty.ToString());
            _processorCount = Environment.ProcessorCount;

            //用 SiteId = 0 取回預設Config
            DefaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(ThirdParty.ToString());
            // 新增商號使用指定siteName
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<Dictionary<string, string>>(ThirdParty.ToString());
        }

        public async Task<TCGReturnObj> SeamlessWallet(string merchantCode, string @params, string sign, string sitename)
        {
            try
            {
                var config = GetSiteConfig(sitename);
                var tcgConfig = new TCGConfig(config, merchantCode);
                var jsonString = DesDecrypt(@params, tcgConfig.DesKey);
                _logger.LogInformation(jsonString);
                var desEncrypt = DesEncrypt(jsonString, tcgConfig.DesKey);
                var checkSign = SHA256($"{desEncrypt}{tcgConfig.SignKey}");
                if (sign != checkSign)
                {
                    throw new TCGException(TCGReturnCode.InvalidSignature, $"Invalid signature | RequestSign is:{sign} | CheckSign is {checkSign}");
                }

                var request = JsonConvert.DeserializeObject<TCGRequest>(jsonString);
                return await _requestHandler[request.method].Invoke(request, tcgConfig);
            }
            catch (TCGException ex)
            {
                return new TCGReturnObj()
                {
                    status = ex.ErrorCode.ToString(),
                    error_desc = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGReturnObj()
                {
                    status = TCGReturnCode.UnknownSystemError.ToString(),
                    error_desc = ex.Message,
                };
            }
        }

        public async Task<TCGReturnObj> SeamlessWallet(string merchantCode, TCGRequest request, string sitename)
        {
            try
            {
                var config = GetSiteConfig(sitename);
                var tcgConfig = new TCGConfig(config, merchantCode);
                return await _requestHandler[request.method].Invoke(request, tcgConfig);
            }
            catch (TCGException ex)
            {
                return new TCGReturnObj()
                {
                    status = ex.ErrorCode.ToString(),
                    error_desc = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGReturnObj()
                {
                    status = TCGReturnCode.UnknownSystemError.ToString(),
                    error_desc = ex.Message,
                };
            }
        }

        private async Task<TCGReturnObj> GetBalance(TCGRequest request, TCGConfig tcgConfig)
        {
            var tpUserName = request.username;
            var (tpCustInfo, tpCurrency) = GetTPInfo(tpUserName);
            var gameType = GetGameType(request.product_type);
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)gameType);
            return new TCGBalanceObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
                balance = tpCustBalance.Balance.ToString(),
            };
        }

        private async Task<TCGReturnObj> Transaction(TCGRequest request, TCGConfig tcgConfig)
        {
            var result = new ConcurrentBag<TCGResult>();
            Parallel.ForEach(
                request.transactions,
                new ParallelOptions() { MaxDegreeOfParallelism = _processorCount },
                async (item) =>
                {
                    result.Add(await ExecuteTransaction(request, item));
                });

            var error = result.FirstOrDefault(item => item.Result != 0);
            if (error != null)
            {
                return new TCGReturnObj
                {
                    status = error.Result.ToString(),
                    error_desc = error.Message,
                };
            }

            return new TCGReturnObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
            };
        }

        private async Task<TCGResult> ExecuteTransaction(TCGRequest request, TCGTransactionData transaction)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(transaction.username);
                transaction.amount *= tpCurrency.ExchangeRate;

                if (request.method == TCGApiMethod.Debit || request.method == TCGApiMethod.Reserve)
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpCustInfo.TpUserName);
                    if (tpCheck == null)
                    {
                        throw new TCGException(TCGReturnCode.InvalidAccountType, $"tpuser {transaction.username} is logout.");
                    }
                    if (tpCheck.Locked)
                    {
                        throw new TCGException(TCGReturnCode.InvalidAccountType, $"tpuser {transaction.username} is locked.");
                    }
                }

                return request.method switch
                {
                    TCGApiMethod.Debit => await bettingService.Debit_TCG(tpCustInfo, GetGameType(request.product_type), request, transaction),
                    TCGApiMethod.Credit => await settledService.Credit_TCG(tpCustInfo, GetGameType(request.product_type), request, transaction),
                    TCGApiMethod.Reserve => await bettingService.Reserve_TCG(tpCustInfo, GetGameType(request.product_type), request, transaction),
                    _ => throw new TCGException(TCGReturnCode.MethodNotFound, $"method:{request.method} not found.")
                };
            }
            catch (TCGException ex)
            {
                return new TCGResult()
                {
                    Result = ex.ErrorCode,
                    Message = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGResult()
                {
                    Result = TCGReturnCode.UnknownSystemError,
                    Message = ex.Message,
                };
            }
        }

        private async Task<TCGReturnObj> Reservation(TCGRequest request, TCGConfig tcgConfig)
        {
            var result = new ConcurrentBag<TCGResult>();
            Parallel.ForEach(
                request.reservation_details,
                new ParallelOptions() { MaxDegreeOfParallelism = _processorCount },
                async (item) =>
                {
                    result.Add(await ExecuteReservation(request, item));
                });

            var error = result.FirstOrDefault(item => item.Result != 0);
            if (error != null)
            {
                return new TCGReturnObj
                {
                    status = error.Result.ToString(),
                    error_desc = error.Message,
                };
            }

            return new TCGReturnObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
            };
        }

        private async Task<TCGResult> ExecuteReservation(TCGRequest request, TCGReservationDetail reservation)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(reservation.username);
                reservation.amount *= tpCurrency.ExchangeRate;

                return request.method switch
                {
                    TCGApiMethod.CancelReserve
                        => await settledService.CancelReserve_TCG(tpCustInfo, GetGameType(request.product_type), request, reservation),
                    TCGApiMethod.ConfirmReserve
                        => await settledService.ConfirmReserve_TCG(tpCustInfo, GetGameType(request.product_type), request, reservation),
                    _ => throw new TCGException(TCGReturnCode.MethodNotFound, $"method:{request.method} not found.")
                };
            }
            catch (TCGException ex)
            {
                return new TCGResult()
                {
                    Result = ex.ErrorCode,
                    Message = ex.Message,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGResult()
                {
                    Result = TCGReturnCode.UnknownSystemError,
                    Message = ex.Message,
                };
            }
        }

        private async Task<TCGReturnObj> CheckTransactionStatus(TCGRequest request, TCGConfig tcgConfig)
        {
            var queryList = new List<TCGQueryResult>();
            foreach (var item in request.transaction_details)
            {
                var bettransLottery = await bettingService.BettransLottery_Get_TCG(item.username, item.ref_no);
                queryList.Add(new TCGQueryResult(item.ref_no, bettransLottery));
            }

            return new TCGCheckObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
                query_result = queryList,
            };
        }

        /// <summary>
        /// DES加密
        /// </summary>
        public string DesEncrypt(string input, string key)
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
        public string DesDecrypt(string pToDecrypt, string key)
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

        public string SHA256(string input)
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
                _ => throw new TCGException(TCGReturnCode.MerchantNotAllowedProductType, $"product_type:{productType} not supported.")
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new TCGException(TCGReturnCode.UserDoesNotExists, $"Customer:{tpUserName} not found.");
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new TCGException(TCGReturnCode.UnSupportedCurrency, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }

        private Dictionary<string, string> GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return DefaultConfig;
            }

            return _siteNameConfigs.TryGetValue(siteName, out var result) ? result : DefaultConfig;
        }

    }
}
