using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class IMBLL : IIMBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpUnsettleService _tpUnsettleService;
        private readonly ILogger<IMBLL> _logger;
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _wagerDbClient;
        private readonly PartitionService _partitionService;

        Dictionary<GameType, TpCategory> _tpGameTypeToTpCategory = new()
        {
            { GameType.IM_Sports, TpCategory.Sports },
            { GameType.IM_ESports, TpCategory.Esports },
            { GameType.SPRIBE_RNG, TpCategory.Slots },
        };

        public IMBLL(IServiceProvider provider)
        {
            _thirdPartyBll = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _partitionService = provider.GetRequiredService<PartitionService>();
            _logger = provider.GetService<ILogger<IMBLL>>();
        }

        public int Bet_IM(List<BetTransactions_IM> betList, string tpUserName, Tp tpId, GameType gameType, out List<BetResultIM> resp)
        {
            var bonusId = 0;
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)tpId, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)tpId, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType);
            var tpCategory = _tpGameTypeToTpCategory[gameType];

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            var result = -1;
            resp = new List<BetResultIM>();

            foreach (var bet in betList)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    bet.Amount *= tpCurrency.ExchangeRate;
                    custBalance.Balance *= tpCurrency.ExchangeRate;
                }

                switch (gameType)
                {
                    case GameType.IM_Sports:
                        var betData = new BetTransactionsSport
                        {
                            PlayerId = bet.PlayerId,
                            ProviderPlayerId = bet.ProviderPlayerId,
                            Provider = bet.Provider,
                            GameId = bet.GameId,
                            GameName = bet.GameName,
                            TransactionId = bet.TransactionId,
                            Currency = bet.Currency,
                            Amount = bet.Amount,
                            Type = bet.Type,
                            TimeStamp = bet.TimeStamp.ToUniversalTime(),
                            BetId = bet.BetId,
                            ActionId = bet.ActionId
                        };
                        var p1 = new DynamicParameters(betData);
                        result = _mainDbClient.Execute("Service_BettransSports_IM_Bet_Ins", p1, conn, trans);
                        break;

                    case GameType.IM_ESports:
                        var eSportData = new BetTransactionsESport
                        {
                            PlayerId = bet.PlayerId,
                            ProviderPlayerId = bet.ProviderPlayerId,
                            Provider = bet.Provider,
                            GameId = bet.GameId,
                            GameName = bet.GameName,
                            TransactionId = bet.TransactionId,
                            Currency = bet.Currency,
                            Amount = bet.Amount,
                            Type = bet.Type,
                            TimeStamp = bet.TimeStamp.ToUniversalTime(),
                            BetId = bet.BetId,
                            ActionId = bet.ActionId,
                            Product = bet.Product
                        };
                        var p2 = new DynamicParameters(eSportData);
                        result = _mainDbClient.Execute("Service_BettransESports_IM_Bet_Ins", p2, conn, trans);
                        break;

                    case GameType.SPRIBE_RNG:
                        var partitionTime = bet.TimeStamp;
                        var slotData = new BetTransactionsSlot
                        {
                            PlayerId = bet.PlayerId,
                            ProviderPlayerId = bet.ProviderPlayerId,
                            Provider = bet.Provider,
                            GameId = bet.GameId,
                            GameName = bet.GameName,
                            RoundId = bet.RoundId,
                            TransactionId = bet.TransactionId,
                            Currency = bet.Currency,
                            Amount = bet.Amount,
                            Type = bet.Type,
                            TimeStamp = bet.TimeStamp.ToUniversalTime(),
                            Platform = bet.Platform
                        };
                        var p3 = new DynamicParameters(slotData);
                        p3.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, "Service_BettransRng_SPRIBE_Bet_Ins", p3, trans);
                        break;
                }

                if (result > 0)
                {
                    result = _bettingService.PlaceBet(
                        tpCustInfo: tpCustInfo,
                        amount: Math.Abs(bet.Amount) * -1,
                        tpId: (int)tpId,
                        tpTransId: gameType == GameType.SPRIBE_RNG ? bet.RoundId : bet.BetId,
                        gameType: (int)gameType,
                        refId: bet.TransactionId,
                        afterAmount: out var afterAmount,
                        bonusId: ref bonusId,
                        useBonus: out _,
                        conn: conn,
                        trans: trans
                    );

                    if (result > 0)
                    {
                        if (afterAmount == 0)
                        {
                            afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
                        }
                        else
                        {
                            if (tpCurrency.ExchangeRate > 0)
                                afterAmount /= tpCurrency.ExchangeRate;
                        }

                        if (gameType != GameType.SPRIBE_RNG)
                        {
                            _tpUnsettleService.InsertTpUnsettle(tpId, gameType, bet.BetId, trans);
                        }

                        var betResult = new BetResultIM
                        {
                            Code = (int)IMResultCode.Success,
                            Message = "Successful",
                            OperatorTransactionId = bet.TransactionId,
                            TransactionId = bet.TransactionId,
                            Balance = Math.Round(afterAmount, 4)
                        };
                        resp.Add(betResult);
                    }

                    if (afterAmount < 0)
                    {
                        // Not enough balance
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return result;
        }

        public int Settle_IM(SettleTransactions_IM data, string tpUserName, Tp tpId, GameType gameType, out object resp)
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)tpId, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)tpId, tpCustInfo.CurrencyId);
            var afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
            var tpCategory = _tpGameTypeToTpCategory[gameType];
            var transId = gameType == GameType.SPRIBE_RNG ? data.RoundId : data.BetId;

            if (tpCurrency.ExchangeRate > 0)
            {
                data.Amount *= tpCurrency.ExchangeRate;
            }

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = -1;
            var successResp = new SettleResultIM
            {
                Code = (int)IMResultCode.Success,
                Message = "Successful",
                TransactionId = data.TransactionId,
                OperatorTransactionId = data.TransactionId
            };
            var errorResponse = new SettleErrorRespIM
            {
                Code = (int)IMResultCode.Exception,
                Message = "Failed to process your request",
                TransactionId = data.TransactionId
            };
            resp = errorResponse;

            try
            {
                switch (gameType)
                {
                    case GameType.IM_Sports:
                        // check exists
                        var checkSport = _mainDbClient.Query<SettleTransactions_IMSport>("Service_BettransSport_IM_Bet_Get", parameters: new { _BetId = data.BetId });
                        if (!checkSport.Any())
                        {
                            return -2;
                        }
                        var sportData = new SettleTransactions_IMSport
                        {
                            PlayerId = data.PlayerId,
                            Provider = data.Provider,
                            GameId = data.GameId,
                            GameName = data.GameName,
                            TransactionId = data.TransactionId,
                            Type = data.Type,
                            Currency = data.Currency,
                            Amount = data.Amount,
                            TimeStamp = data.TimeStamp.ToUniversalTime(),
                            BetId = data.BetId,
                            ActionId = data.ActionId,
                            EventId = data.EventId
                        };
                        var p1 = new DynamicParameters(sportData);
                        result = _mainDbClient.Execute("Service_BettransSports_IM_Settle_Ins", p1, conn, trans);
                        break;

                    case GameType.IM_ESports:
                        // check exists
                        var checkEsport = _mainDbClient.Query<SettleTransactions_IMESport>("Service_BettransESport_IM_Bet_Get", parameters: new { _BetId = data.BetId });
                        if (!checkEsport.Any())
                        {
                            return -2;
                        }
                        var eSportData = new SettleTransactions_IMESport
                        {
                            PlayerId = data.PlayerId,
                            Provider = data.Provider,
                            GameId = data.GameId,
                            GameName = data.GameName,
                            TransactionId = data.TransactionId,
                            Type = data.Type,
                            Currency = data.Currency,
                            Amount = data.Amount,
                            TimeStamp = data.TimeStamp.ToUniversalTime(),
                            BetId = data.BetId,
                            ActionId = data.ActionId,
                            Product = data.Product,
                            EventId = data.EventId
                        };
                        var p2 = new DynamicParameters(eSportData);
                        result = _mainDbClient.Execute("Service_BettransESports_IM_Settle_Ins", p2, conn, trans);
                        break;

                    case GameType.SPRIBE_RNG:
                        var partitionTime = data.TimeStamp;
                        // check exists
                        var checkSpribe = _partitionService.Query<SettleTransactions_IMSlot>(partitionTime, "Service_BettransRng_SPRIBE_Round_Get", new { RoundId = data.RoundId }, 30);
                        if (!checkSpribe.Any())
                        {
                            return -2;
                        }
                        var slotData = new SettleTransactions_IMSlot
                        {
                            PlayerId = data.PlayerId,
                            Provider = data.Provider,
                            GameId = data.GameId,
                            GameName = data.GameName,
                            TransactionId = data.TransactionId,
                            Type = data.Type,
                            Currency = data.Currency,
                            Amount = data.Amount,
                            TimeStamp = data.TimeStamp.ToUniversalTime(),
                            RoundId = data.RoundId,
                            RefTransactionId = JsonConvert.SerializeObject(data.RefTransactionId),
                            SpecialGame = JsonConvert.SerializeObject(data.SpecialGame)
                        };
                        var p3 = new DynamicParameters(slotData);
                        p3.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, "Service_BettransRng_SPRIBE_Settle_Ins", p3, trans);
                        break;
                }

                if (result > 0)
                {
                    var transType = TransType.Payout;
                    if (gameType != GameType.SPRIBE_RNG)
                    {
                        transType = GetTransType(data.ActionId);
                    }

                    if ((gameType != GameType.SPRIBE_RNG && data.Amount != 0) ||
                         (gameType == GameType.SPRIBE_RNG && data.Amount > 0))
                    {
                        result = _settledService.PayoutV2(
                            tpCustInfo,
                            data.Amount,
                            (int)tpId,
                            transId,
                            (int)gameType,
                            tpCategory,
                            data.TransactionId,
                            transType,
                            null,
                            out afterAmount,
                            gameType != GameType.SPRIBE_RNG ? Enum.GetName(typeof(IMActionId), data.ActionId) : "Payout",
                            out var bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }

                    if (result > 0)
                    {
                        if (afterAmount == 0)
                        {
                            afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
                        }
                        else
                        {
                            if (tpCurrency.ExchangeRate > 0)
                                afterAmount /= tpCurrency.ExchangeRate;
                        }

                        result = _tpUnsettleService.InsertTpUnsettle(tpId, gameType, transId, trans);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);

                            successResp.Balance = afterAmount;
                            resp = successResp;
                            return result;
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                            return result;
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return result;
            }

            return result;
        }

        public int Refund_SPRIBE(RefundTransaction_Spribe data, string tpUserName, Tp tpId, GameType gameType, out object resp)
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)tpId, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)tpId, tpCustInfo.CurrencyId);
            var afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
            var tpCategory = _tpGameTypeToTpCategory[gameType];
            var transId = data.RefTransactionId;

            var successResp = new SettleResultIM
            {
                Code = (int)IMResultCode.Success,
                Message = "Successful",
                TransactionId = data.TransactionId,
                OperatorTransactionId = data.TransactionId
            };
            var errorResponse = new SettleErrorRespIM
            {
                Code = (int)IMResultCode.Exception,
                Message = "Failed to process your request",
                TransactionId = data.TransactionId
            };
            resp = errorResponse;

            var partitionTime = data.TimeStamp;
            var betData = _partitionService.Query<SettleTransactions_IMSlot>(partitionTime, "Service_BettransRng_SPRIBE_Round_Get", new { RoundId = transId }, 30).FirstOrDefault();
            if (betData is null)
            {
                return -2;
            }

            if (tpCurrency.ExchangeRate > 0)
            {
                betData.Amount *= tpCurrency.ExchangeRate;
            }

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = -1;
            try
            {
                var slotData = new SettleTransactions_IMSlot
                {
                    PlayerId = data.PlayerId,
                    Provider = data.Provider,
                    GameId = betData.GameId,
                    GameName = betData.GameName,
                    TransactionId = data.TransactionId,
                    Type = data.TransactionType,
                    Currency = betData.Currency,
                    Amount = betData.Amount,
                    TimeStamp = data.TimeStamp.ToUniversalTime(),
                    RoundId = data.RefTransactionId,
                    RefTransactionId = JsonConvert.SerializeObject(new List<string> { data.RefTransactionId }),
                    SpecialGame = JsonConvert.SerializeObject(betData.SpecialGame)
                };
                var p3 = new DynamicParameters(slotData);
                p3.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_SPRIBE_Settle_Ins", p3, trans);

                if (result > 0)
                {
                    if (betData.Amount > 0)
                    {
                        result = _settledService.PayoutV2(
                            tpCustInfo,
                            betData.Amount,
                            (int)tpId,
                            transId,
                            (int)gameType,
                            tpCategory,
                            data.TransactionId,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Refund",
                            out var bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }

                    if (result > 0)
                    {
                        if (afterAmount == 0)
                        {
                            afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
                        }
                        else
                        {
                            if (tpCurrency.ExchangeRate > 0)
                                afterAmount /= tpCurrency.ExchangeRate;
                        }

                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        successResp.Balance = afterAmount;
                        resp = successResp;
                        return result;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }

                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return result;
            }

            return result;
        }

        public int SetSettleFlag_BettransRng_IM(string spName, string transId, DateTime settleTime)
        {
            var partitionTime = settleTime;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            var result = _partitionService.Execute(partitionTime, spName, new DynamicParameters(new { TransId = transId }), trans);
            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;

        }

        public int SetSettleFlag_Bettrans_IM(string spName, string transId)
        {
            var result = _mainDbClient.Execute(spName, parameters: new DynamicParameters(new { TransId = transId }));
            return result;
        }

        public int SetSettleFlag_TpWager_IM(string spName, string transId)
        {
            var result = _wagerDbClient.Execute(spName, parameters: new DynamicParameters(new { TransId = transId }));
            return result;
        }

        public TransType GetTransType(int actionId)
        {
            var transType = TransType.Payout;

            switch (actionId)
            {
                case (int)IMActionId.PlaceBetRefund:
                case (int)IMActionId.DangerRefund:
                case (int)IMActionId.LiveBallRefund:
                case (int)IMActionId.CancelHT:
                case (int)IMActionId.CancelFT:
                case (int)IMActionId.CancelOutright:
                case (int)IMActionId.CancelSingleWager:
                case (int)IMActionId.CancelParlay:
                    transType = TransType.Refunds;
                    break;

                case (int)IMActionId.SettleHT:
                case (int)IMActionId.SettleFT:
                case (int)IMActionId.SettleOutright:
                case (int)IMActionId.SettleParlay:
                case (int)IMActionId.SettleOnHold:
                case (int)IMActionId.BTBuyBack:
                case (int)IMActionId.BTRestoreBuyBack:
                    transType = TransType.Payout;
                    break;

                case (int)IMActionId.BTCancelBuyBack:
                case (int)IMActionId.LiveBallRefundRevert:
                case (int)IMActionId.UnsettleHT:
                case (int)IMActionId.UnsettleFT:
                case (int)IMActionId.UnsettleOutright:
                case (int)IMActionId.UnsettleParlay:
                case (int)IMActionId.UnsettleOnHold:
                case (int)IMActionId.UncancelHT:
                case (int)IMActionId.UncancelFT:
                case (int)IMActionId.UncancelOutright:
                case (int)IMActionId.UncancelSingleWager:
                case (int)IMActionId.UncancelParlay:
                    transType = TransType.Undo;
                    break;
            }

            return transType;
        }
    }
}