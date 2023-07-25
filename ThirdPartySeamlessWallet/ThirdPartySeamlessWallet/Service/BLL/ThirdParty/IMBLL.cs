using Dapper;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class IMBLL : IIMBLL
    {
        private readonly Tp _thisTp;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<HBBLL> _logger;
        private readonly MySqlClient _mainDbClient;

        public IMBLL(IServiceProvider provider)
        {
            _thisTp = Tp.IM;
            _thirdPartyBll = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _logger = provider.GetService<ILogger<HBBLL>>();
        }
        
        public int Bet_IM(List<Transactions_IM> betList ,string tpUserName, GameType gameType , out List<BetResultIM> resp)
        {
            var bonusId = 0;
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)Tp.IM, tpUserName);
            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)Tp.IM, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBll.GetBalance((int)Tp.IM, tpUserName, (int)gameType);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();
            
            var result = -1;
            resp = new List<BetResultIM>();
            var tpCategory = gameType switch
            {
                GameType.IM_Sport =>  TpCategory.Sports,
                GameType.IM_Esport =>  TpCategory.Esports,
                GameType.IM_Spribe =>  TpCategory.Slots ,
                _ => TpCategory.Sports
            };

            foreach (var bet in betList)
            {
                if (tpCurrency.ExchangeRate > 0)
                {
                    bet.Amount *= tpCurrency.ExchangeRate;
                    custBalance.Balance *= tpCurrency.ExchangeRate;
                }

                switch (gameType)
                {
                    case GameType.IM_Sport:
                        var betData = new TransactionsSport
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

                    case GameType.IM_Esport:
                        var eSportData = new TransactionsESport
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

                    case GameType.IM_Spribe:
                        var slotData = new TransactionsSlot
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
                        result = _mainDbClient.Execute("Service_BettransRng_IM_Bet_Ins", p3, conn, trans);
                        break;
                }

                if (result > 0)
                {
                    result = _bettingService.PlaceBetV2(
                        tpCustInfo: tpCustInfo,
                        amount: Math.Abs(bet.Amount) * -1,
                        tpId: (int)Tp.IM,
                        tpTransId: bet.TransactionId,
                        gameType: (int)gameType,
                        refId: bet.TransactionId,
                        betData: null,
                        tpCategory: tpCategory,
                        afterAmount: out var afterAmount,
                        bonusId: ref bonusId,
                        conn: conn,
                        trans: trans
                    );
                
                    if (result > 0)
                    {
                        if (afterAmount == 0)
                        {
                            afterAmount = _tpCustInfoBll.GetBalance((int)_thisTp, tpUserName, (int)gameType).Balance;
                        }
                        var betResult = new BetResultIM
                        {
                            Code = (int)IMResultCode.Success,
                            Message = "Successful",
                            OperatorTransactionId = bet.TransactionId,
                            TransactionId = bet.TransactionId,
                            Balance = Math.Round( afterAmount, 4)
                        };
                        resp.Add(betResult);
                    }

                    if (afterAmount < 0)
                    {
                        // Not enough balance
                        _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                        return -1;
                    } 
                    
                }
            }
            
            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
            }
            
            return result;
        }

    }
    
}


