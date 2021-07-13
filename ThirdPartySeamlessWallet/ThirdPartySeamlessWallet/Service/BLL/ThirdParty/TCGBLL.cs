using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public readonly ThirdPartySetting thirdPartySetting;
        private readonly Dictionary<string, Func<TCGRequest, TCGConfig, Task<TCGReturnObj>>> _requestHandler;
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
            _requestHandler = new Dictionary<string, Func<TCGRequest, TCGConfig, Task<TCGReturnObj>>>()
            {
                { TCGApiMethod.GetBalance, GetBalance},
                { TCGApiMethod.Debit, Transaction},
                { TCGApiMethod.Credit, Transaction},
                { TCGApiMethod.Reserve, Transaction},
                { TCGApiMethod.CancelReserve, CancelReserve},
                { TCGApiMethod.ConfirmReserve, ConfirmReserve},
                { TCGApiMethod.CheckTransactionStatus, CheckTransactionStatus},
            };
        }

        public async Task<TCGReturnObj> SeamlessWallet(string merchantCode, string @params, string sign)
        {
            try
            {
                var tcgConfig = new TCGConfig(thirdPartySetting.TpSetting, merchantCode);
                var jsonString = DesDecrypt(@params, tcgConfig.DesKey);
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
            var Tasks = request.transactions.Select(item => Task.Run(() => ExecuteTransaction(request, item))).ToArray();
            var result = await Task.WhenAll(Tasks);

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

        private async Task<TCGResult> ExecuteTransaction(TCGRequest request , TransactionData transactionData)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(transactionData.username);
                transactionData.amount *= tpCurrency.ExchangeRate;

                return request.method switch
                {
                    TCGApiMethod.Debit => await bettingService.Debit_TCG(tpCustInfo, GetGameType(request.product_type), request, transactionData),
                    TCGApiMethod.Credit => await settledService.Credit_TCG(tpCustInfo, GetGameType(request.product_type), request, transactionData),
                    TCGApiMethod.Reserve => await bettingService.Reserve_TCG(tpCustInfo, GetGameType(request.product_type), request, transactionData),
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
                return new TCGResult()
                {
                    Result = TCGReturnCode.UnknownSystemError,
                    Message = ex.Message,
                };
            }
        }

        private async Task<TCGReturnObj> CancelReserve(TCGRequest request, TCGConfig tcgConfig)
        {
            return new TCGReturnObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
            };
        }

        private async Task<TCGReturnObj> ConfirmReserve(TCGRequest request, TCGConfig tcgConfig)
        {
            return new TCGReturnObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
            };
        }

        private async Task<TCGReturnObj> CheckTransactionStatus(TCGRequest request, TCGConfig tcgConfig)
        {
            return new TCGReturnObj
            {
                status = TCGReturnCode.Success.ToString(),
                error_desc = "Success",
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
      
    }
}
