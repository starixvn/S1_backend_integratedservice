using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class KMService : IKMService, IThirdPartyBLL
    {
        public Tp ThirdParty  { get; set; }
        public GameType GameType { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<KMService> _logger;
        private Dictionary<string, string> Config;
        
        public KMService(IServiceProvider provider)
        {
            ThirdParty = Tp.KM;
            GameType = GameType.KM_Poker;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<KMService>>();
            Config = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty).TpSetting;
        }

        public bool CheckCredentials(string clientId, string clientSecret)
        {
            return string.Equals(clientId, Config["X-QM-ClientId"]) &&
                   string.Equals(clientSecret, Config["X-QM-ClientSecret"]);
        }

        public async Task<KMDebitResponse> Debit(KMDebitRequest request)
        {
            var response = new KMDebitResponse()
            {
                transactions = new List<KMDebitResponseTransaction>()
            };
            foreach (var transaction in request.transactions)
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, transaction.UserId);
                if (tpCheck == null || tpCheck.Locked)
                {
                    _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                    throw new KmException(KMErrorCode.UserBlocked);
                }
                
                if (Enum.IsDefined(typeof(KMTxType), transaction.TxType))
                {
                    var transactionResult = await Bet(transaction);

                    response.transactions.Add(transactionResult);
                }
            }
            return response;
        }

        public async Task<KMCreditResponse> Credit(KMCreditRequest request)
        {
            var response = new KMCreditResponse()
            {
                transactions = new List<KMCreditResponseTransaction>()
            };
            foreach (var transaction in request.transactions)
            {
                if (Enum.IsDefined(typeof(KMTxType), transaction.txtype))
                {
                    KMCreditResponseTransaction transactionResult;
                    if (transaction.txtype == (int)KMTxType.CancelTransaction ||
                        transaction.txtype == (int)KMTxType.CancelFundOut)
                    {
                        transactionResult = await Cancel(transaction);
                    }
                    else
                    {
                        transactionResult = await Payout(transaction);
                    }
                    
                    response.transactions.Add(transactionResult);
                }
            }
            return response;
        }

        public KMBalanceResponse GetBalance(KMBalanceRequest request)
        {
            var response = new KMBalanceResponse()
            {
                users = new List<KMUsers>()
            };
            foreach (var user in request.users)
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, user.UserId);
                if (tpCheck == null || tpCheck.Locked)
                {
                    _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                    throw new KmException(KMErrorCode.UserBlocked);
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, user.UserId, (int)GameType);
                if (tpCustBalance != null)
                {
                    var kmUser = new KMUsers()
                    {
                        userid = user.UserId,
                        wallets = new List<UserWallet>()
                        {
                            new UserWallet()
                            {
                                code = "MainWallet",
                                bal = tpCustBalance.Balance,
                                cur = tpCustBalance.TpCurrencyId
                            }
                        }
                    };
                    response.users.Add(kmUser);
                }
            }
            return response;
        }

        private async Task<KMDebitResponseTransaction> Bet(KMDebitRequestTransaction request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.UserId);
            request.amt *= tpCurrency.ExchangeRate;

            var dbResult = new KmDbResult();

            if (Enum.IsDefined(typeof(KMGameCode), request.GameCode) && request.TxType.ToString().StartsWith("5"))
            {
                dbResult = await _bettingService.PlaceBetNoSetBalance_KM(request);

            }
            else
            {
                dbResult = await _bettingService.PlaceBetPK_KM(tpCustInfo, request, (int) ThirdParty, (int) GameType);
            }
            
            // 訂單已存在
            if (dbResult.AfterAmount == 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.UserId, (int)GameType);
                dbResult.AfterAmount = tpCustBalance.Balance;
                
                return new KMDebitResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.PtxId,
                    bal = dbResult.AfterAmount,
                    cur = tpCurrency.TpCurrencyId,
                    dup = dbResult.SPResult == 0
                };
            }

            if (dbResult.AfterAmount > 0)
            {
                dbResult.AfterAmount /= tpCurrency.ExchangeRate;
            }
            return new KMDebitResponseTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.PtxId,
                bal = dbResult.AfterAmount,
                cur = tpCurrency.TpCurrencyId,
                dup = dbResult.SPResult == 0
            };
        }

        private async Task<KMCreditResponseTransaction> Payout(KMCreditRequestTransaction request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.userid);
            request.amt *= tpCurrency.ExchangeRate;

            var dbResult = new KmDbResult();
            if (Enum.IsDefined(typeof(KMGameCode), request.gamecode) && request.txtype.ToString().StartsWith("5"))
            {
                dbResult = await _settledService.PayoutNoSetBalance_KM(request);
            }
            else
            {
                dbResult = await _settledService.PayoutPK_KM(tpCustInfo, request);
            }

            //交易不存在
            if (dbResult.SPResult == 2)
            {
                return new KMCreditResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.ptxid,
                    err = (int)KMErrorCode.TransactionDoesNotExist,
                    errdesc = KMErrorCode.TransactionDoesNotExist.GetEnumDescription()
                };
            }

            // 訂單已存在
            if (dbResult.AfterAmount == 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.userid, (int)GameType);
                dbResult.AfterAmount = tpCustBalance.Balance;
                
                return new KMCreditResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.ptxid,
                    bal = dbResult.AfterAmount,
                    cur = tpCurrency.TpCurrencyId,
                    dup = dbResult.SPResult == 0
                };
            }

            if (dbResult.AfterAmount > 0)
            {
                dbResult.AfterAmount /= tpCurrency.ExchangeRate;
            }
            return new KMCreditResponseTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.ptxid,
                bal = dbResult.AfterAmount,
                cur = tpCurrency.TpCurrencyId,
                dup = dbResult.SPResult == 0
            };
        }

        private async Task<KMCreditResponseTransaction> Cancel(KMCreditRequestTransaction request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.userid);
            request.amt *= tpCurrency.ExchangeRate;

            var dbResult = await _settledService.CancelbetPK_KM(tpCustInfo, request);

            //交易不存在
            if (dbResult.SPResult == 2)
            {
                return new KMCreditResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.ptxid,
                    err = (int)KMErrorCode.TransactionDoesNotExist,
                    errdesc = KMErrorCode.TransactionDoesNotExist.GetEnumDescription()
                };
            }

            // 訂單已存在
            if (dbResult.AfterAmount == 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.userid, (int)GameType);
                dbResult.AfterAmount = tpCustBalance.Balance;
                
                return new KMCreditResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.ptxid,
                    bal = dbResult.AfterAmount,
                    cur = tpCurrency.TpCurrencyId,
                    dup = dbResult.SPResult == 0
                };
            }

            if (dbResult.AfterAmount > 0)
            {
                dbResult.AfterAmount /= tpCurrency.ExchangeRate;
            }
            return new KMCreditResponseTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.ptxid,
                bal = dbResult.AfterAmount,
                cur = tpCurrency.TpCurrencyId,
                dup = dbResult.SPResult == 0
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                _logger.LogError($"tpCustInfo is null, tpUserName:{tpUserName}");
                throw new KmException(KMErrorCode.UserBlocked);
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                _logger.LogError($"tpCurrency is null, {JsonConvert.SerializeObject(tpCustInfo)}");
                throw new KmException(KMErrorCode.CurrencyMismatch);
            }
            return (tpCustInfo, tpCurrency);
        }
    }
}