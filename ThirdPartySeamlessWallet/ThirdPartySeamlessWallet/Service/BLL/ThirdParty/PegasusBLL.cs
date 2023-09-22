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
    public class PegasusBLL : IPegasusBLL
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
        

        public PegasusBLL(IServiceProvider provider)
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

        public int Bet_Pegasus(BettingRequest_Pegasus bet, string tpUserName, Tp tpId, GameType gameType, out ResultPegasus resp)
        {
            var bonusId = 0;
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)tpId, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)tpId, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType);

            var result = 0;
            resp = new ResultPegasus();
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                
               if (tpCurrency.ExchangeRate > 0) 
               { 
                   bet.BetAmount *= tpCurrency.ExchangeRate;
                   custBalance.Balance *= tpCurrency.ExchangeRate;
               }

                var partitionTime = DateTime.Now.ToUniversalTime();
                var bettingData = new BetTransaction_Pegasus
                {
                    PlayerUniqueID = bet.PlayerUniqueID,
                    GameID = bet.GameID,
                    BetAmount = bet.BetAmount,
                    TransactionNo = bet.TransactionNo,
                    ExtraInfo = JsonConvert.SerializeObject(bet.ExtraInfo)
                };
                
                var p = new DynamicParameters(bettingData);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_Pegasus_Bet_Ins", p, trans);

                if (result > 0)
                {
                    result = _bettingService.PlaceBetV2(
                        tpCustInfo: tpCustInfo,
                        amount: Math.Abs(bettingData.BetAmount) * -1,
                        tpId: (int)tpId,
                        tpTransId: bettingData.TransactionNo,
                        gameType: (int)gameType,
                        refId: bettingData.TransactionNo,
                        betData: null,
                        tpCategory: TpCategory.Slots,
                        afterAmount: out var afterAmount,
                        bonusId: ref bonusId,
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

                        resp.Balance = Math.Round(afterAmount, 2);
                        resp.TransactionNo = bettingData.TransactionNo;
                    }

                    if (afterAmount < 0)
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return -2;
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
            catch (MySqlException mySqlEx)
            {
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    return -1;
                }
            }
            return result;
        }
        
        public int Settle_Pegasus(SettleRequest_Pegasus settle, string tpUserName, Tp tpId, GameType gameType, out ResultPegasus resp)
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)tpId, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)tpId, tpCustInfo.CurrencyId);
            var afterAmount = _tpCustInfoBll.GetBalance((int)tpId, tpUserName, (int)gameType).Balance;
            
            if (tpCurrency.ExchangeRate > 0)
            {
                settle.PayoutAmount *= tpCurrency.ExchangeRate;
            }

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = -1;
            resp = new ResultPegasus();
          
            try
            {
                var partitionTime = settle.SettlementTime;
                // check exists
                var checkExits = _partitionService.Query<BetTransaction_Pegasus>(partitionTime, "Service_BettransRng_Pegasus_Bet_Get", new { TransactionNo = settle.TransactionNo }, 2);
                if (!checkExits.Any())
                {
                    return -2;
                }
                var settleData = new SettleTransaction_Pegasus
                {
                    TransactionNo = settle.TransactionNo,
                    GameID = settle.GameID,
                    BetAmount = settle.BetAmount,
                    TaxAmount = settle.TaxAmount,
                    PayoutAmount = settle.PayoutAmount,
                    ValidAmount = settle.ValidAmount,
                    SettlementTime = settle.SettlementTime,
                    PlayerUniqueID = settle.PlayerUniqueID,
                    ExtraInfo = JsonConvert.SerializeObject(settle.ExtraInfo),
                    CreateTime = DateTime.UtcNow
                };
                var p = new DynamicParameters(settleData);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_Pegasus_Settle_Ins", p, trans);

                if (result > 0)
                {
                    if (settle.PayoutAmount > 0)
                    {
                        result = _settledService.PayoutV2(
                            tpCustInfo,
                            settle.PayoutAmount,
                            (int)tpId,
                            settle.TransactionNo,
                            (int)gameType,
                            TpCategory.Slots,
                            settle.TransactionNo,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
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

                        result = _tpUnsettleService.InsertTpUnsettle(tpId, gameType, settle.TransactionNo, trans);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);

                            resp.Balance = afterAmount;
                            resp.TransactionNo = settle.TransactionNo;
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
                    result = -3;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return result;
            }

            return result;
        }
    }
}