using MySql.Data.MySqlClient;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class Kiss918BLL : IKiss918BLL
    {
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<Kiss918BLL> _logger;

        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpRoundGenService _tpRoundGenService;

        private readonly MySqlClient _mainDbClient;

        public Kiss918BLL(IServiceProvider provider)
        {
            ThirdParty = Tp.Kiss918;
            GameType = GameType.Kiss918_RNG;

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<Kiss918BLL>>();

            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpRoundGenService = provider.GetRequiredService<TpRoundGenService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<AuthenticateResponse_Kiss918> Authenticate(AuthenticateRequest_Kiss918 request)
        {
            var message = string.Empty;

            if (request == null
                || string.IsNullOrWhiteSpace(request.userName)
                || string.IsNullOrWhiteSpace(request.password))
            {
                message = request == null ? "request is invaild." : "userName or password is invaild.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return new AuthenticateResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);
            }

            if (!CheckPlayerByUserName(request, out int playerID, out Kiss918ErrorCode errorCode, out message))
                return new AuthenticateResponse_Kiss918(errorCode, message);

            if (!CheckThirdParty(request.userName, out message))
                return new AuthenticateResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);


            if (!GetBalance(request.userName, out decimal balance, out message))
                return new AuthenticateResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            return new AuthenticateResponse_Kiss918(Kiss918ErrorCode.Success, "", playerID, balance);
        }

        public async Task<GetBalanceResponse_Kiss918> GetBalance(int playerID)
        {
            var message = string.Empty;

            if (playerID == 0)
            {
                message = "playerID is InVaild.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return new GetBalanceResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);
            }

            if (!GetTpInfoByPlayerID(playerID, out TpCustInfo tpCustInfo, out message))
                return new GetBalanceResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!GetBalance(tpCustInfo.TpUserName, out decimal balance, out message))
                return new GetBalanceResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            return new GetBalanceResponse_Kiss918(Kiss918ErrorCode.Success, "", balance);
        }

        public async Task<OperationResponse_Kiss918> Bet(BetRequest_Kiss918 request)
        {
            var message = string.Empty;
            decimal balance = 0;

            if (!CheckRequestForBet(request, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (!GetTpInfoByPlayerID(request.playerID, out TpCustInfo tpCustInfo, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            var partitionTime = DateTime.UtcNow;

            var data = new BettingDataRNG_Kiss918()
            {
                CustId = tpCustInfo.CustId,
                playerID = request.playerID,
                gameID = request.gameID,
                playSessionID = request.playSessionID,
                referenceID = request.referenceID,
                amount = Math.Abs(request.betAmount) * -1,
                roundDetails = request.roundDetails,
                platform = request.platform,
                FromAPIType = BettransRng_Kiss918TypeEnum.FromBet,
                TicketType = BettransType.Bet,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            if (!IsExistData(partitionTime, data, out IEnumerable<BettingDataRNG_Kiss918> existDatas, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (existDatas.Any())
            {
                //已存在指定有效投注
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "already exist this data.", request.referenceID, balance);
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceData(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            if (data.amount != 0)
            {
                string newRoundId = string.Empty;
                if (!IsExistNewRoundId(data.playSessionID, out newRoundId, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                if (string.IsNullOrWhiteSpace(newRoundId))
                {
                    if (!GenNewRoundId(data.playSessionID, out newRoundId, out message, trans))
                    {
                        trans.Rollback();
                        return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                    }
                }

                data.NewRoundId = newRoundId;

                if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                }
            }
            else
            {
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            trans.Commit();
            Thread.Sleep(60 * 1000); //sleep 60 sec for test refund
            //return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "", request.referenceID, balance);
            return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "", request.referenceID, balance);
        }

        public async Task<OperationResponse_Kiss918> BetResult(BetResultRequest_Kiss918 request)
        {
            var message = string.Empty;
            string newRoundId = string.Empty;
            decimal balance = 0;

            if (!CheckRequestForBetResult(request, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!GetTpInfoByPlayerID(request.playerID, out TpCustInfo tpCustInfo, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            var partitionTime = DateTime.UtcNow;

            var data = new BettingDataRNG_Kiss918()
            {
                CustId = tpCustInfo.CustId,
                playerID = request.playerID,
                gameID = request.gameID,
                playSessionID = request.playSessionID,
                referenceID = request.referenceID,
                amount = Math.Abs(request.winAmount),
                jackpotModule = request.jackpotModule,
                jackpotContributionAmt = request.jackpotContributionAmt,
                resultUrl = request.resultUrl,
                roundDetails = request.roundDetails,
                platform = request.platform,
                FromAPIType = BettransRng_Kiss918TypeEnum.FromBetResult,
                TicketType = BettransType.Payout,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!IsExistData(partitionTime, data, out IEnumerable<BettingDataRNG_Kiss918> existDatas, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (existDatas.Any())
            {
                //已存在指定有效投注
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "already exist this data.", request.referenceID, balance);
            }

            if (!PlaceData(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            if (data.amount != 0)
            {
                if (!IsExistNewRoundId(data.playSessionID, out newRoundId, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                if (string.IsNullOrWhiteSpace(newRoundId))
                {
                    if (!GenNewRoundId(data.playSessionID, out newRoundId, out message, trans))
                    {
                        trans.Rollback();
                        return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                    }
                }

                data.NewRoundId = newRoundId;

                if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                }
            }
            else
            {
                //lost => 沒有派彩，balance取db資料
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }


            if (!InsertUnsettleData(partitionTime, newRoundId, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            trans.Commit();

            return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "", request.referenceID, balance);
        }

        public async Task<OperationResponse_Kiss918> Refund(RefundRequest_Kiss918 request)
        {
            var message = string.Empty;
            decimal balance = 0;

            if (!CheckRequestForRefund(request, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (!GetTpInfoByPlayerID(request.playerID, out TpCustInfo tpCustInfo, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            var partitionTime = DateTime.UtcNow;

            var data = new BettingDataRNG_Kiss918()
            {
                CustId = tpCustInfo.CustId,
                playerID = request.playerID,
                gameID = request.gameID,
                playSessionID = request.playSessionID,
                referenceID = request.referenceID,
                amount = Math.Abs(request.betAmount),
                platform = request.platform,
                FromAPIType = BettransRng_Kiss918TypeEnum.FromRefund,
                TicketType = BettransType.CancelBet,
                TicketStatus = TicketStatus.Refund
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            if (!IsExistData(partitionTime, data, out IEnumerable<BettingDataRNG_Kiss918> existDatas, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (existDatas.Any())
            {
                //已存在指定有效投注
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "already exist this data.", request.referenceID, balance);
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceData(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            if (data.amount != 0)
            {
                string newRoundId = string.Empty;
                if (!IsExistNewRoundId(data.playSessionID, out newRoundId, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                if (string.IsNullOrWhiteSpace(newRoundId))
                {
                    if (!GenNewRoundId(data.playSessionID, out newRoundId, out message, trans))
                    {
                        trans.Rollback();
                        return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                    }
                }

                data.NewRoundId = newRoundId;

                if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                }
            }
            else
            {
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            trans.Commit();

            return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "", request.referenceID, balance);
        }

        public async Task<OperationResponse_Kiss918> JackpotWin(JackpotWinRequest_Kiss918 request)
        {
            var message = string.Empty;
            string newRoundId = string.Empty;
            decimal balance = 0;

            if (!CheckRequestForBetResult(request, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!GetTpInfoByPlayerID(request.playerID, out TpCustInfo tpCustInfo, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayer, message);

            if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            var partitionTime = DateTime.UtcNow;

            var data = new BettingDataRNG_Kiss918()
            {
                CustId = tpCustInfo.CustId,
                playerID = request.playerID,
                gameID = request.gameID,
                playSessionID = request.playSessionID,
                referenceID = request.referenceID,
                amount = Math.Abs(request.winAmount),
                jackpotModule = request.jackpotModule,
                jackpotContributionAmt = request.jackpotContributionAmt,
                resultUrl = request.resultUrl,
                roundDetails = request.roundDetails,
                platform = request.platform,
                FromAPIType = BettransRng_Kiss918TypeEnum.FromJackpotWin,
                TicketType = BettransType.Jackpot,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!IsExistData(partitionTime, data, out IEnumerable<BettingDataRNG_Kiss918> existDatas, out message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

            if (existDatas.Any())
            {
                //已存在指定有效投注
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "already exist this data.", request.referenceID, balance);
            }

            if (!PlaceData(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            if (data.amount != 0)
            {
                if (!IsExistNewRoundId(data.playSessionID, out newRoundId, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);

                if (string.IsNullOrWhiteSpace(newRoundId))
                {
                    if (!GenNewRoundId(data.playSessionID, out newRoundId, out message, trans))
                    {
                        trans.Rollback();
                        return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                    }
                }

                data.NewRoundId = newRoundId;

                if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
                }
            }
            else
            {
                //lost => 沒有派彩，balance取db資料
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }


            if (!InsertUnsettleData(partitionTime, newRoundId, out message, trans))
            {
                trans.Rollback();
                return new OperationResponse_Kiss918(Kiss918ErrorCode.ServiceNotAvailable, message);
            }

            trans.Commit();

            return new OperationResponse_Kiss918(Kiss918ErrorCode.Success, "", request.referenceID, balance);
        }

        #region Private Methods

        private bool CheckRequestForBet(BetRequest_Kiss918 request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (request.playerID == 0)
            {
                message = "request.appid is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.playSessionID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.referenceID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }

        private bool CheckRequestForBetResult(BetResultRequest_Kiss918 request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (request.playerID == 0)
            {
                message = "request.appid is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.playSessionID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.referenceID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.resultUrl))
            {
                message = "request.resultUrl is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }

        private bool CheckRequestForRefund(RefundRequest_Kiss918 request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (request.playerID == 0)
            {
                message = "request.appid is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.playSessionID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.referenceID))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }


            return true;
        }

        private bool CheckPlayerByUserName(AuthenticateRequest_Kiss918 request, out int playerId, out Kiss918ErrorCode errorCode, out string message)
        {
            playerId = 0;
            errorCode = Kiss918ErrorCode.Success;
            message = string.Empty;

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.userName);
            if (tpCustInfo == null)
            {
                message = "playerID data is null.";
                _logger.LogError($"CheckPlayerByUserName Fail:{message}");
                errorCode = Kiss918ErrorCode.InvaildPlayer;
                return false;
            }

            if (tpCustInfo.TpPwd != request.password)
            {
                message = "Password invaild.";
                _logger.LogError($"CheckPlayerByUserName Fail:{message}");
                errorCode = Kiss918ErrorCode.InvaildPlayerAuth;
                return false;
            }

            if (string.IsNullOrWhiteSpace(tpCustInfo.TpCustId))
            {
                message = "playerID is null.";
                _logger.LogError($"CheckPlayerByUserName Fail:{message}");
                errorCode = Kiss918ErrorCode.InvaildPlayer;
                return false;
            }

            if (!int.TryParse(tpCustInfo.TpCustId, out int number))
            {
                message = "playerID is not a number.";
                _logger.LogError($"CheckPlayerByUserName Fail:{message}");
                errorCode = Kiss918ErrorCode.InvaildPlayerAuth;
                return false;
            }

            playerId = number;
            return true;
        }

        private bool CheckThirdParty(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

                if (result == null)
                {
                    message = "player not found.";
                    _logger.LogError($"CheckThirdParty Fail:{message}");
                    return false;
                }

                if (result.Locked)
                {
                    message = "player is locked.";
                    _logger.LogError($"CheckThirdParty Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckThirdParty Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpInfoByPlayerID(int playerID, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCacheByTpCustId((int)ThirdParty, playerID.ToString());
                if (tpCustInfo == null)
                {
                    message = "playerID data is null.";
                    _logger.LogError($"CheckUserNameByPlayerID Fail:{message}");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(tpCustInfo.TpUserName))
                {
                    message = "TpUserName is null.";
                    _logger.LogError($"CheckUserNameByPlayerID Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckUserNameByPlayerID Exception:{ex.Message}");
                return false;
            }
        }

        private bool GetTpCurrency(int currencyId, out TpCurrency tpCurrency, out string message)
        {
            message = string.Empty;
            tpCurrency = null;
            try
            {
                tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, currencyId);
                if (tpCurrency == null)
                {
                    message = "tpCurrency is null.";
                    _logger.LogError($"GetTpCurrency Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetTpCurrency Exception:{ex.Message}");
                return false;
            }
        }

        private bool GetBalance(string username, out decimal balance, out string message)
        {
            message = string.Empty;
            balance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username, (int)GameType);

                if (result == null)
                {
                    message = "balance not found.";
                    _logger.LogError($"GetBalance Fail:{message}");
                    return false;
                }
                balance = result.Balance;
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetBalance Exception:{ex.Message}");
                return false;
            }
        }

        private bool IsExistData(DateTime partitionTime, BettingDataRNG_Kiss918 data, out IEnumerable<BettingDataRNG_Kiss918> existDatas, out string message)
        {
            message = string.Empty;
            existDatas = null;
            try
            {
                existDatas = _bettingService.IsExist_Kiss918(partitionTime, data);

                if (existDatas.Any())
                {
                    //已存在指定有效投注
                    message = "already exist valid bet by referenceID.";
                    _logger.LogError($"CheckBet Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckBet Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool PlaceData(DateTime partitionTime, BettingDataRNG_Kiss918 data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            var result = false;
            try
            {
                switch (data.FromAPIType)
                {
                    case BettransRng_Kiss918TypeEnum.FromBet:
                        result = _bettingService.PlaceBet_Kiss918(partitionTime, data, trans);
                        break;
                    case BettransRng_Kiss918TypeEnum.FromBetResult:
                    case BettransRng_Kiss918TypeEnum.FromJackpotWin:
                        result = _settledService.Payout_Kiss918(partitionTime, data, trans);
                        break;
                    case BettransRng_Kiss918TypeEnum.FromRefund:
                        result = _settledService.Refund_Kiss918(partitionTime, data, trans);
                        break;
                }

                if (!result)
                {
                    message = "place payout fail.";
                    _logger.LogError($"PlacePayout Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceReward Exception:{ex.Message}");
                return false;
            }
        }

        private bool SetBalance(TpCustInfo tpCustInfo, BettingDataRNG_Kiss918 data, decimal rate,
                                out string message, out decimal afterAmount, out int bonusId,
                                MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            var result = false;
            try
            {
                switch (data.FromAPIType)
                {
                    case BettransRng_Kiss918TypeEnum.FromBet:
                        result = _bettingService.SetBalance_Kiss918(tpCustInfo, data,
                                 out afterAmount, ref bonusId, conn, trans);
                        break;
                    case BettransRng_Kiss918TypeEnum.FromBetResult:
                        result = _settledService.SetBalance_Kiss918(tpCustInfo, data,
                                 TransType.Payout, "Payout", out afterAmount, ref bonusId, conn, trans);
                        break;
                    case BettransRng_Kiss918TypeEnum.FromRefund:
                        result = _settledService.SetBalance_Kiss918(tpCustInfo, data,
                                 TransType.Payout, "Refund", out afterAmount, ref bonusId, conn, trans);
                        break;
                    case BettransRng_Kiss918TypeEnum.FromJackpotWin:
                        result = _settledService.SetBalance_Kiss918(tpCustInfo, data,
                                 TransType.Payout, "Jackpot", out afterAmount, ref bonusId, conn, trans);
                        break;
                }

                if (!result)
                {
                    message = "set balance fail.";
                    _logger.LogError($"SetBalance Fail:{message}");
                    return false;
                }

                afterAmount = (rate > 0) ? afterAmount / rate : afterAmount;

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"SetBalance Exception:{ex.Message}");
                return false;
            }
        }
        private bool InsertUnsettleData(DateTime partitionTime, string playSessionID, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.InsertUnsettle_Kiss918(partitionTime, playSessionID, trans);

                if (!result)
                {
                    message = "insert unsettle transactionId fail.";
                    _logger.LogError($"InsertUnsettle Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"InsertUnsettle Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool IsExistNewRoundId(string playSessionID, out string newRoundId, out string message)
        {
            message = string.Empty;
            newRoundId = string.Empty;
            try
            {
                var existDatas = _tpRoundGenService.GetExistRoundId(ThirdParty, GameType.Kiss918_RNG, playSessionID);

                if (existDatas.Any())
                    newRoundId = existDatas.First().SQ.ToString();

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckBet Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GenNewRoundId(string playSessionID, out string newRoundId, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            newRoundId = string.Empty;
            try
            {
                var newRoundData = new TpRoundIdGen
                {
                    TpId = ThirdParty,
                    GameType = GameType.Kiss918_RNG,
                    TpRoundId = playSessionID
                };

                var result = _tpRoundGenService.GenNewRoundId(newRoundData, trans);

                if (result == 0)
                {
                    message = "place payout fail.";
                    _logger.LogError($"PlacePayout Fail:{message}");
                    return false;
                }
                newRoundId = result.ToString();
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceReward Exception:{ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
