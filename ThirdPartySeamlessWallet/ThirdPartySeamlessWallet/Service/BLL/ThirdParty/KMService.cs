using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly Dictionary<KMTxType, Func<KMDebitRequestTransaction, Task<KMDebitTransaction>>> _debitHandler; 
        
        public KMService(IServiceProvider provider)
        {
            ThirdParty = Tp.KM;
            GameType = GameType.KM_TableGame;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _debitHandler = new Dictionary<KMTxType, Func<KMDebitRequestTransaction, Task<KMDebitTransaction>>>()
            {
                {KMTxType.PlaceBet, Bet}
            };
        }

        public async Task<KMDebitResponse> Debit(KMDebitRequest request)
        {
            var response = new KMDebitResponse()
            {
                transactions = new List<KMDebitTransaction>()
            };
            foreach (var transaction in request.transactions)
            {
                var a = (KMTxType) transaction.TxType;
                var transactionResult = await _debitHandler[(KMTxType) transaction.TxType].Invoke(transaction);
                if (request.transactional && transactionResult == null)
                {
                    throw new KmException(KMErrorCode.IncorrectFormat);
                }
                response.transactions.Add(transactionResult);
            }
            return response;
        }

        public void Credit()
        {
            throw new System.NotImplementedException();
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

        private async Task<KMDebitTransaction> Bet(KMDebitRequestTransaction request)
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
                return new KMDebitTransaction()
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
            return new KMDebitTransaction()
            {
                txid = dbResult.Trx_id.ToString(),
                ptxid = request.PtxId,
                bal = dbResult.AfterAmount,
                cur = request.Currency,
                dup = false
            };
        }
    }
}