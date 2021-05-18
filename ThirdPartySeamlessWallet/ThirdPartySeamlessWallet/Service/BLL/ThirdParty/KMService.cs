using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class KMService : IKMService, IThirdPartyService
    {
        public Tp ThirdParty  { get; set; }
        public GameType GameType { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<KMService> _logger;
        private readonly Dictionary<KMTxType, Func<KMDebitRequestTransaction, Task<KMDebitResponseTransaction>>> _debitHandler; 
        private readonly Dictionary<KMTxType, Func<KMCreditRequestTransaction, Task<KMCreditResponseTransaction>>> _creditHandler; 
        
        public KMService(IServiceProvider provider)
        {
            ThirdParty = Tp.KM;
            GameType = GameType.KM_Poker;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<KMService>>();
            _debitHandler = new Dictionary<KMTxType, Func<KMDebitRequestTransaction, 
                Task<KMDebitResponseTransaction>>>()
            {
                {KMTxType.PlaceBet, Bet}
            };
            _creditHandler = new Dictionary<KMTxType, Func<KMCreditRequestTransaction, 
                Task<KMCreditResponseTransaction>>>()
            {
                {KMTxType.WinBet, WinBet}
            };
        }

        public async Task<KMDebitResponse> Debit(KMDebitRequest request)
        {
            var response = new KMDebitResponse()
            {
                transactions = new List<KMDebitResponseTransaction>()
            };
            foreach (var transaction in request.transactions)
            {
                if (Enum.IsDefined(typeof(KMTxType), transaction.TxType))
                {
                    var transactionResult = await _debitHandler[(KMTxType) transaction.TxType].Invoke(transaction);
                    if (request.transactional && transactionResult == null)
                    {
                        throw new KmException(KMErrorCode.IncorrectFormat);
                    }

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
                    var transactionResult = await WinBet(transaction);
                    if (transactionResult == null)
                    {
                        throw new KmException(KMErrorCode.IncorrectFormat);
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
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.UserId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

            // if (tpCurrency.ExchangeRate > 0)
            // {
            //     request.Amount *= tpCurrency.ExchangeRate;
            // }

            var dbResult =
                await _bettingService.PlaceBetPK_KM(tpCustInfo, request, (int) ThirdParty, (int) GameType);
            // 訂單已存在
            if (dbResult.SPResult == 0)
            {
                return new KMDebitResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.PtxId,
                    dup = true
                };
            }

            if (dbResult.AfterAmount > 0)
            {
                request.Amount /= tpCurrency.ExchangeRate;
                dbResult.AfterAmount /= tpCurrency.ExchangeRate;
            }
            return new KMDebitResponseTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.PtxId,
                bal = dbResult.AfterAmount,
                cur = request.Currency,
                dup = false
            };
        }

        private async Task<KMCreditResponseTransaction> WinBet(KMCreditRequestTransaction request)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.userid);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

            // if (tpCurrency.ExchangeRate > 0)
            // {
            //     request.Amount *= tpCurrency.ExchangeRate;
            // }

            var dbResult = await _settledService.PayoutPK_KM(tpCustInfo, request, (int) ThirdParty, (int) GameType);
            // 訂單已存在
            if (dbResult.SPResult == 0)
            {
                return new KMCreditResponseTransaction()
                {
                    txid = dbResult.Trx_id.ToString(),
                    ptxid = request.ptxid,
                    dup = true
                };
            }

            if (dbResult.AfterAmount > 0)
            {
                request.amt /= tpCurrency.ExchangeRate;
                dbResult.AfterAmount /= tpCurrency.ExchangeRate;
            }
            return new KMCreditResponseTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.ptxid,
                bal = dbResult.AfterAmount,
                cur = request.cur,
                dup = false
            };
        }
    }
}