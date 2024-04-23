using MySql.Data.MySqlClient;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class JokerBLL : IJokerBLL, IThirdPartyBLL
    {
        #region Fields

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<JokerBLL> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;

        #endregion

        #region Constructors
        public JokerBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.Joker;
            GameType = GameType.Joker_RNG;

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<JokerBLL>>();

            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        public async Task<JokerResponse> JoinTournament(JoinAndWinRequest request)
        {
            var message = string.Empty;
            decimal balance = 0;

            if (!CheckRequest(false, request, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!CheckThirdParty(request.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCustInfo(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = request.appid,
                hash = request.hash,
                id = request.id,
                tournamentid = request.tournamentid,
                amount = Math.Abs(request.amount) * -1, //Join為減項
                username = request.username,
                extendedinfo = request.extendedinfo,
                timestamp = request.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromJoin,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            if (!GetExistReward(false, data, out IEnumerable<RewardRNG_Joker> existDatas, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (existDatas.Any())
            {
                //已存在指定有效投注
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }

                return new JokerResponse()
                {
                    Status = JokerErrorCode.Success,
                    Message = "already exist valid join by username and id.",
                    Balance = balance
                };
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceReward(RewardRNG_JokerTypeEnum.FromJoin, data, out message, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (data.amount != 0)
            {
                if (!SetBalance(RewardRNG_JokerTypeEnum.FromJoin, tpCustInfo, data, tpCurrency.ExchangeRate,
                                out message, out balance, out int bonusId, conn, trans))
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }
            else
            {
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }

            await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

            return new JokerResponse()
            {
                Status = JokerErrorCode.Success,
                Message = message,
                Balance = balance
            };
        }

        public async Task<JokerResponse> WinTournament(JoinAndWinRequest request)
        {
            var message = string.Empty;
            decimal balance = 0;

            if (!CheckRequest(false, request, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!GetTpCustInfo(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = request.appid,
                hash = request.hash,
                id = request.id,
                tournamentid = request.tournamentid,
                amount = Math.Abs(request.amount),
                username = request.username,
                extendedinfo = request.extendedinfo,
                timestamp = request.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromWin,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            if (!GetExistReward(false, data, out IEnumerable<RewardRNG_Joker> existDatas, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (existDatas.Any())
            {
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }

                //已存在指定有效派彩
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Success,
                    Message = "already exist valid win by username and id.",
                    Balance = balance
                };
            }


            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceReward(RewardRNG_JokerTypeEnum.FromWin, data, out message, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (data.amount != 0)
            {
                if (!SetBalance(RewardRNG_JokerTypeEnum.FromWin, tpCustInfo, data, tpCurrency.ExchangeRate,
                                out message, out balance, out int bonusId, conn, trans))
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }
            else
            {
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }

            await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

            return new JokerResponse()
            {
                Status = JokerErrorCode.Success,
                Message = message,
                Balance = balance
            };
        }

        public async Task<JokerResponse> CancelTournament(CancelRequest request)
        {
            var message = string.Empty;
            decimal balance = 0;

            if (!CheckRequest(true, request, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!GetTpCustInfo(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = request.appid,
                hash = request.hash,
                id = request.id,
                tournamentid = request.jointournamentid,
                username = request.username,
                extendedinfo = request.extendedinfo,
                timestamp = request.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromJoin,
                TicketType = BettransType.CancelBet,
                TicketStatus = TicketStatus.Refund
            };

            if (!GetExistReward(true, data, out IEnumerable<RewardRNG_Joker> existDatas, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (!existDatas.Any())
            {
                //不存在指定有效資料
                message = "not exist join by username and id.";
                _logger.LogError($"CheckExist Fail:{message}");
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (existDatas.Count() > 1)
            {
                //存在多筆指定有效資料
                message = "exist multi join by username and id.";
                _logger.LogError($"CheckExist Fail:{message}");
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            var joinData = existDatas.First();

            if (joinData.TicketType == BettransType.CancelBet
                && joinData.TicketStatus == TicketStatus.Refund)
            {
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }

                //該筆資料已被取消
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Success,
                    Message = "already cancel assign join by username and id.",
                    Balance = balance
                };
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceReward(RewardRNG_JokerTypeEnum.FromCancel, joinData, out message, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            if (joinData.amount != 0)
            {
                joinData.amount = Math.Abs(joinData.amount); //原本存在table的數字已經是換算過匯率的數字
                //joinData.id = data.id;                     //Cancel的id太長(length=51)，沒辦法塞到FinancialLog(50)

                if (!SetBalance(RewardRNG_JokerTypeEnum.FromCancel, tpCustInfo, joinData, tpCurrency.ExchangeRate,
                                out message, out balance, out int bonusId, conn, trans))
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }
            else
            {
                if (!GetBalance(request.username, out balance, out message))
                {
                    return new JokerResponse()
                    {
                        Status = JokerErrorCode.Other,
                        Message = message
                    };
                }
            }

            await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

            return new JokerResponse()
            {
                Status = JokerErrorCode.Success,
                Message = message,
                Balance = balance
            };
        }

        #region Private Methods

        private bool CheckRequest(bool isCancel, dynamic request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.appid))
            {
                message = "request.appid is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.hash))
            {
                message = "request.hash is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.id))
            {
                message = "request.id is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.username))
            {
                message = "request.username is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.extendedinfo))
            {
                message = "request.extendedinfo is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (isCancel)
            {
                if (string.IsNullOrWhiteSpace(request.jointournamentid))
                {
                    message = "request.jointournamentid is Invalid.";
                    _logger.LogError($"CheckRequest Fail:{message}");
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.tournamentid))
                {
                    message = "request.tournamentid is Invalid.";
                    _logger.LogError($"CheckRequest Fail:{message}");
                    return false;
                }
            }

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
                    message = "player is Locked.";
                    _logger.LogError($"CheckThirdParty Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckThirdParty Exception:{ex.Message}");
                return false;
            }
        }

        private bool GetTpCustInfo(string username, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
                if (tpCustInfo == null)
                {
                    message = "tpCustInfo is null.";
                    _logger.LogError($"GetTpCustInfo Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.Message}");
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

        private bool GetExistReward(bool isCancel, RewardRNG_Joker data, out IEnumerable<RewardRNG_Joker> existDatas, out string message)
        {
            message = string.Empty;
            existDatas = null;
            try
            {
                existDatas = _bettingService.GetRewardRNG_Joker(isCancel, data);

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetExistReward Exception:{ex.Message}");
                return false;
            }
        }

        private bool PlaceReward(RewardRNG_JokerTypeEnum type, RewardRNG_Joker data, out string message, MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            var result = false;
            try
            {
                switch (type)
                {
                    case RewardRNG_JokerTypeEnum.FromJoin:
                        result = _bettingService.InsertReward_Joker(data, conn, trans);
                        break;
                    case RewardRNG_JokerTypeEnum.FromWin:
                        result = _settledService.InsertReward_Joker(data, conn, trans);
                        break;
                    case RewardRNG_JokerTypeEnum.FromCancel:
                        result = _settledService.CancelUpdateReward_Joker(data.SQ, conn, trans);
                        break;
                }

                if (!result)
                {
                    _logger.LogError($"PlaceReward Fail:{message}");
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

        private bool SetBalance(RewardRNG_JokerTypeEnum type, TpCustInfo tpCustInfo, RewardRNG_Joker data, decimal rate,
                                out string message, out decimal afterAmount, out int bonusId,
                                MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            var result = false;
            try
            {
                switch (type)
                {
                    case RewardRNG_JokerTypeEnum.FromJoin:
                        result = _bettingService.PlaceBetReward_Joker(tpCustInfo, data,
                                 out afterAmount, ref bonusId, conn, trans);
                        break;
                    case RewardRNG_JokerTypeEnum.FromCancel:
                        result = _settledService.PayoutReward_Joker(tpCustInfo, data,
                                 TransType.Refunds, "Cancel Tournament",
                                 out afterAmount, ref bonusId, conn, trans);
                        break;
                    case RewardRNG_JokerTypeEnum.FromWin:
                        result = _settledService.PayoutReward_Joker(tpCustInfo, data,
                                 TransType.Payout, "Win Tournament",
                                 out afterAmount, ref bonusId, conn, trans);
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

        #endregion
    }
}
