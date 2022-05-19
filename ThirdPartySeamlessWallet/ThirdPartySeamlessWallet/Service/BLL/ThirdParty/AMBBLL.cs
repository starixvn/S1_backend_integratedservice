using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class AMBBLL : IAMBBLL, IThirdPartyBLL
    {
        #region Fields

        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<AMBBLL> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;

        public string[] HasEndroundTableGameId { get; set; }

        #endregion

        #region Constructors

        public AMBBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.AMB;
            GameType = GameType.AMB_RNG;
            HasEndroundTableGameId = new string[] { "BACCARAT", "DRAGONTIGER" };

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<AMBBLL>>();

            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        #region Methods

        public AMBSlotGetBalanceResponse SlotGetBalance(ANBSlotGetBalanceRequest request)
        {
            var message = string.Empty;

            if (!CheckRequestForSlotGetBalance(request, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }
            
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, request.username, (int)GameType);
            if (tpCustBalance == null)
            {
                _logger.LogError($"GetBalance Fail: balance not found.");
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = "Service not available" }
                };
            }
            
            //不確定會是用agentId還是anget，當參數檢查通過時兩個值互補
            var agent = string.IsNullOrWhiteSpace(request.agent) ? request.agentId : request.agent;

            if (!CheckAgentId(agent, tpCustBalance.TpCurrencyId))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = "Invalid agent id" }
                };
            }

            if (!CheckThirdPartyForSlotGetBalance(request.username, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            return new AMBSlotGetBalanceResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBalance() { balance = tpCustBalance.Balance }
            };
        }

        public async Task<AMBSlotBetResponse> SlotBet(AMBSlotBetRequest request)
        {
            var message = string.Empty;
            decimal beforeBalance = 0; decimal afterBalance = 0;

            if (!CheckRequestForSlotBet(request, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            //不確定會是用agentId還是anget，當參數檢查通過時兩個值互補
            var agent = string.IsNullOrWhiteSpace(request.agent) ? request.agentId : request.agent;

            if (!CheckAgentId(agent, request.currency))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = "Invalid agent id" }
                };
            }

            if (!CheckThirdPartyForSlotBet(request.username, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            if (!GetBalanceForSlotBet(request.username, out beforeBalance, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (!CheckBeforeBalanceForSlotBet(request.amount, beforeBalance, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.BalanceInsufficient, message = message }
                };
            }

            if (!GetTpCustInfoForSlotBet(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (!GetTpCurrencyForSlotBet(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            var partitionTime = DateTimeOffset.Parse(request.timestamp).DateTime;

            var data = new BettingDataRNG_AMB()
            {
                username = request.username,
                agentId = agent,
                game = request.game,
                gameId = request.gameId,
                roundId = request.roundId,
                amount = Math.Abs(request.amount) * -1,  //Bet為減項
                roundAmount = request.roundAmount ?? 0,
                currency = request.currency,
                transactionId = request.transactionId,
                timestamp = partitionTime,
                featureBuy = request.featureBuy,
                isEndRound = request.isEndRound,
                type = request.type,
                tranId = request.tranId,
                FromAPIType = BettransRng_AMBTypeEnum.FromBet,
                TicketType = BettransType.Bet,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!CheckBetForSlotBet(partitionTime, data, out message))
            {
                trans.Rollback();
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.DuplicateRound, message = message }
                };
            }

            if (!PlaceBetForSlotBet(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (data.amount != 0)
            {
                if (!SetBalanceForSlotBet(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new AMBSlotBetResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                    };
                }
            }

            trans.Commit();

            return new AMBSlotBetResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = beforeBalance, after = afterBalance },
                    refId = request.transactionId
                }
            };
        }

        public async Task<AMBSlotPayoutResponse> SlotPayout(AMBSlotPayoutRequest request)
        {
            var message = string.Empty;
            decimal beforeBalance = 0; decimal afterBalance = 0;

            if (!CheckRequestForSlotPayout(request, out message))
            {
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            //不確定會是用agentId還是anget，當參數檢查通過時兩個值互補
            var agent = string.IsNullOrWhiteSpace(request.agent) ? request.agentId : request.agent;

            if (!CheckAgentId(agent, request.currency))
            {
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = "Invalid agent id" }
                };
            }

            if (!CheckThirdPartyForSlotPayout(request.username, out message))
            {
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            if (!GetTpCustInfoForSlotPayout(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (!GetTpCurrencyForSlotPayout(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            var partitionTime = DateTimeOffset.Parse(request.timestamp).DateTime;

            var data = new BettingDataRNG_AMB()
            {
                username = request.username,
                agentId = agent,
                game = request.game,
                gameId = request.gameId,
                roundId = request.roundId,
                amount = Math.Abs(request.amount),
                turnover = request.turnover,
                roundAmount = request.roundAmount ?? 0,
                currency = request.currency,
                transactionId = request.transactionId,
                timestamp = partitionTime,
                featureBuy = request.featureBuy,
                isEndRound = request.isEndRound,
                type = request.type,
                tranId = request.tranId,
                FromAPIType = BettransRng_AMBTypeEnum.FromPayout,
                TicketType = request.isEndRound ? BettransType.EndRound : BettransType.Payout,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!CheckPayoutForSlotPayout(partitionTime, data, out message))
            {
                trans.Rollback();
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.DuplicateRound, message = message }
                };
            }

            if (!PlacePayoutForSlotPayout(partitionTime, data, out message, trans))
            {
                trans.Rollback();
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (data.amount != 0)
            {
                if (!SetBalanceForSlotPayout(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new AMBSlotPayoutResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                    };
                }
                //win => 有派彩，beforeBalance用afterBalance反推
                beforeBalance = afterBalance - request.amount;
            }
            else
            {
                //lost => 沒有派彩，balance取db資料
                if (!GetBalanceForSlotPayout(request.username, out beforeBalance, out message))
                {
                    return new AMBSlotPayoutResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                    };
                }
                afterBalance = beforeBalance;
            }

            if (!CheckSettleForSlotPayout(data.gameId, out bool doSettle, out message))
            {
                trans.Rollback();
                return new AMBSlotPayoutResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (doSettle || data.isEndRound)
            {
                if (!InsertUnsettleForSlotPayout(partitionTime, data.roundId, out message, trans))
                {
                    trans.Rollback();
                    return new AMBSlotPayoutResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                    };
                }
            }

            trans.Commit();

            return new AMBSlotPayoutResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = beforeBalance, after = afterBalance },
                    refId = request.transactionId
                }
            };
        }

        public async Task<AMBSlotCancelResponse> SlotCancel(AMBSlotCancelRequest request)
        {
            var message = string.Empty;
            decimal beforeBalance = 0; decimal afterBalance = 0;

            if (!CheckRequestForSlotCancel(request, out message))
            {
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            //不確定會是用agentId還是anget，當參數檢查通過時兩個值互補
            var agent = string.IsNullOrWhiteSpace(request.agent) ? request.agentId : request.agent;

            if (!CheckAgentId(agent, request.currency))
            {
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = "Invalid agent id" }
                };
            }

            if (!GetTpCustInfoForSlotCancel(request.username, out TpCustInfo tpCustInfo, out message))
            {
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (!GetTpCurrencyForSlotCancel(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            {
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            var partitionTime = DateTimeOffset.Parse(request.timestamp).DateTime;

            var data = new BettingDataRNG_AMB()
            {
                username = request.username,
                agentId = agent,
                game = request.game,
                gameId = request.gameId,
                roundId = request.roundId,
                amount = Math.Abs(request.amount),
                currency = request.currency,
                transactionId = request.transactionId,
                timestamp = partitionTime,
                TicketType = BettransType.CancelBet,
                TicketStatus = TicketStatus.UnSettled,
                FromAPIType = BettransRng_AMBTypeEnum.FromBet
            };

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

            if (!CheckCancelForSlotCancel(partitionTime, data, out int sq, out message))
            {
                trans.Rollback();
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.DuplicateRound, message = message }
                };
            }

            if (!CancelBetForSlotCancel(partitionTime, sq, out message, trans))
            {
                trans.Rollback();
                return new AMBSlotCancelResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (data.amount != 0)
            {
                if (!SetBalanceForSlotCancel(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out afterBalance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new AMBSlotCancelResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                    };
                }
            }

            trans.Commit();

            return new AMBSlotCancelResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = afterBalance, lastUpdate = GetCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = afterBalance - request.amount, after = afterBalance },
                    refId = request.transactionId
                }
            };
        }

        #endregion

        #region Private Methods

        #region Get Balance Setps

        private bool CheckRequestForSlotGetBalance(ANBSlotGetBalanceRequest request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.agent) && string.IsNullOrWhiteSpace(request.agentId))
            {
                message = "request.agent is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.username))
            {
                message = "request.username is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }
        private bool CheckThirdPartyForSlotGetBalance(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

                if (result == null)
                {
                    message = "player not found.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                if (result.Locked)
                {
                    message = "player is locked.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetBalanceForSlotGetBalance(string username, out decimal balance, out string message)
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
                _logger.LogError(ex, $"GetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        #endregion

        #region Bet Steps

        private bool CheckRequestForSlotBet(AMBSlotBetRequest request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.username))
            {
                message = "request.username is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.agent) && string.IsNullOrWhiteSpace(request.agentId))
            {
                message = "request.agent is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.game))
            {
                message = "request.game is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameId))
            {
                message = "request.gameId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.roundId))
            {
                message = "request.roundId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.currency))
            {
                message = "request.currency is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.transactionId))
            {
                message = "request.transactionId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.timestamp))
            {
                message = "request.timestamp is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }

        private bool CheckThirdPartyForSlotBet(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

                if (result == null)
                {
                    message = "player not found.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                if (result.Locked)
                {
                    message = "player is Locked.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetBalanceForSlotBet(string username, out decimal beforeBalance, out string message)
        {
            message = string.Empty;
            beforeBalance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalance((int)ThirdParty, username, (int)GameType);

                if (result == null)
                {
                    message = "balance not found.";
                    _logger.LogError($"GetBalance Fail:{message}");
                    return false;
                }

                beforeBalance = result.Balance;
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckBeforeBalanceForSlotBet(decimal betAmount, decimal beforeBalance, out string message)
        {
            message = string.Empty;

            if (beforeBalance - betAmount < 0)
            {
                message = "Balance insufficient.";
                _logger.LogError($"CheckBalance Fail:{message}");
                return false;
            }

            return true;
        }

        private bool GetTpCustInfoForSlotBet(string username, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, username);
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
                _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpCurrencyForSlotBet(int currencyId, out TpCurrency tpCurrency, out string message)
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
                _logger.LogError(ex, $"GetTpCurrency Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckBetForSlotBet(DateTime partitionTime, BettingDataRNG_AMB data, out string message)
        {
            message = string.Empty;
            try
            {
                var slotDatas = _bettingService.IsExist_AMB(false, partitionTime, data);

                if (slotDatas.Any())
                {
                    //已存在指定有效投注
                    message = "already exist valid bet by transactionId and roundId.";
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

        private bool PlaceBetForSlotBet(DateTime partitionTime, BettingDataRNG_AMB data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _bettingService.PlaceBet_AMB(partitionTime, data, trans);

                if (!result)
                {
                    message = "place bet fail.";
                    _logger.LogError($"PlaceBet Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceBet Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool SetBalanceForSlotBet(TpCustInfo tpCustInfo, BettingDataRNG_AMB data, decimal rate,
            out string message, out decimal afterAmount, out int bonusId,
            MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            try
            {
                var result = _bettingService.SetBalance_AMB(tpCustInfo, data, out afterAmount, ref bonusId, conn, trans);

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
                _logger.LogError(ex, $"SetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        #endregion

        #region Payout Steps

        private bool CheckRequestForSlotPayout(AMBSlotPayoutRequest request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.username))
            {
                message = "request.username is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.agent) && string.IsNullOrWhiteSpace(request.agentId))
            {
                message = "request.agent is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.game))
            {
                message = "request.game is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameId))
            {
                message = "request.gameId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.roundId))
            {
                message = "request.roundId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.currency))
            {
                message = "request.currency is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.transactionId))
            {
                message = "request.transactionId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.timestamp))
            {
                message = "request.timestamp is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }

        private bool CheckThirdPartyForSlotPayout(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

                if (result == null)
                {
                    message = "player not found.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                if (result.Locked)
                {
                    message = "player is Locked.";
                    _logger.LogError($"CheckTpCustInfo Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetBalanceForSlotPayout(string username, out decimal beforeBalance, out string message)
        {
            message = string.Empty;
            beforeBalance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalance((int)ThirdParty, username, (int)GameType);

                if (result == null)
                {
                    message = "balance not found.";
                    _logger.LogError($"GetBalance Fail:{message}");
                    return false;
                }

                beforeBalance = result.Balance;
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"GetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpCustInfoForSlotPayout(string username, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, username);
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
                _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpCurrencyForSlotPayout(int currencyId, out TpCurrency tpCurrency, out string message)
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
                _logger.LogError(ex, $"GetTpCurrency Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckPayoutForSlotPayout(DateTime partitionTime, BettingDataRNG_AMB data, out string message)
        {
            message = string.Empty;
            try
            {
                var slotDatas = _bettingService.IsExist_AMB(false, partitionTime, data);


                if (slotDatas.Any())
                {
                    //已存在指定有效派彩
                    message = "already exist valid payout by transactionId and roundId.";
                    _logger.LogError($"CheckPayout Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckPayout Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool PlacePayoutForSlotPayout(DateTime partitionTime, BettingDataRNG_AMB data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.Payout_AMB(partitionTime, data, trans);

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
                _logger.LogError(ex, $"PlacePayout Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool SetBalanceForSlotPayout(TpCustInfo tpCustInfo, BettingDataRNG_AMB data, decimal rate,
            out string message, out decimal afterAmount, out int bonusId,
            MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            try
            {
                var result = _settledService.SetBalance_AMB(tpCustInfo, data,
                    TransType.Payout, "Payout",
                    out afterAmount, ref bonusId, conn, trans);

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
                _logger.LogError(ex, $"SetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckSettleForSlotPayout(string gameId, out bool doSettle, out string message)
        {
            message = string.Empty;
            doSettle = false;
            try
            {
                doSettle = _thirdPartyBLL.GetGameCasinoWithCache((int)ThirdParty).Any(x => x.TypeId == 3 && x.GameId == gameId);

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $" CheckSettle Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool InsertUnsettleForSlotPayout(DateTime partitionTime, string roundId, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.InsertUnsettle_AMB(partitionTime, roundId, trans);

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

        #endregion

        #region Cancel Steps

        private bool CheckRequestForSlotCancel(AMBSlotCancelRequest request, out string message)
        {
            message = string.Empty;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.username))
            {
                message = "request.username is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.agent) && string.IsNullOrWhiteSpace(request.agentId))
            {
                message = "request.agent is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.game))
            {
                message = "request.game is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.gameId))
            {
                message = "request.gameId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.roundId))
            {
                message = "request.roundId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.currency))
            {
                message = "request.currency is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.transactionId))
            {
                message = "request.transactionId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.timestamp))
            {
                message = "request.timestamp is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                return false;
            }

            return true;
        }

        private bool GetTpCustInfoForSlotCancel(string username, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, username);
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
                _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpCurrencyForSlotCancel(int currencyId, out TpCurrency tpCurrency, out string message)
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
                _logger.LogError(ex, $"GetTpCurrency Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckCancelForSlotCancel(DateTime partitionTime, BettingDataRNG_AMB data, out int sq, out string message)
        {
            message = string.Empty;
            sq = 0;
            try
            {
                var slotDatas = _bettingService.IsExist_AMB(false, partitionTime, data);

                if (!slotDatas.Any())
                {
                    //不存在指定有效投注
                    message = "not exist bet by transactionId and roundId.";
                    _logger.LogError($"CheckCancel Fail:{message}");
                    return false;
                }

                if (slotDatas.Count() > 1)
                {
                    //存在多筆指定有效投注
                    message = "already exist multi bet by transactionId and roundId.";
                    _logger.LogError($"CheckCancel Fail:{message}");
                    return false;
                }

                var endRoundDatas = _bettingService.IsExist_AMB(true, partitionTime, data);

                if (endRoundDatas.Any())
                {
                    //該回合已結束，不可取消投注
                    message = "roundId already end. can't cancel this round's bet.";
                    _logger.LogError($"CheckCancel Fail:{message}");
                    return false;
                }

                sq = slotDatas.First().SQ;
                return true;

            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckCancel Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CancelBetForSlotCancel(DateTime partitionTime, int sq,
            out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.Cancel_AMB(partitionTime, sq, trans);

                if (!result)
                {
                    message = "place bet fail.";
                    _logger.LogError($"PlaceBet Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceBet Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool SetBalanceForSlotCancel(TpCustInfo tpCustInfo, BettingDataRNG_AMB data, decimal rate,
            out string message, out decimal afterAmount, out int bonusId,
            MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            try
            {
                var result = _settledService.SetBalance_AMB(tpCustInfo, data,
                    TransType.Refunds, "CancelBet",
                    out afterAmount, ref bonusId, conn, trans);

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
                _logger.LogError(ex, $"SetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        #endregion

        private string GetCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }

        private bool CheckAgentId(string agentId, string currencyId)
        {
            try
            {
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);

                if (thirdPartySetting == null)
                {
                    _logger.LogError("CheckAgentId Fail: tpinfo not found.");
                    return false;
                }

                var slotAgent = thirdPartySetting.TpSetting[$"SlotAgent{currencyId}"];

                if (string.IsNullOrWhiteSpace(slotAgent))
                {
                    _logger.LogError("CheckAgentId Fail: tpinfo.geant is null.");
                    return false;
                }

                if (slotAgent != agentId)
                {
                    _logger.LogError($"CheckAgentId Fail: {slotAgent} is not equal {agentId}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CheckAgentId Exception:{ex.Message}");
                return false;
            }
        }

        #endregion

    }
}
