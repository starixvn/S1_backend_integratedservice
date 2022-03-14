using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
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

        #endregion

        #region Constructors

        public AMBBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.AMB;
            GameType = GameType.AMB_RNG;

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<AMBBLL>>();

            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        #region Methods

        public async Task<AMBSlotGetBalanceResponse> SlotGetBalance(ANBSlotGetBalanceRequest request)
        {
            var message = string.Empty;

            if (!CheckRequestForSlotGetBalance(request, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            if (!CheckAgentIdForSlotGetBalance(request.agent, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = message }
                };
            }

            if (!CheckTpCustInfoForSlotGetBalance(request.username, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            if (!GetBalanceForSlotGetBalance(request.username, out decimal balance, out message))
            {
                return new AMBSlotGetBalanceResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            return new AMBSlotGetBalanceResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBalance() { balance = balance }
            };
        }

        public async Task<AMBSlotBetResponse> SlotBet(AMBSlotBetRequest request)
        {
            var message = string.Empty;

            if (!CheckRequestForSlotBet(request, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            if (!CheckAgentIdForSlotBet(request.agent, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidAgent, message = message }
                };
            }

            if (!CheckTpCustInfoForSlotBet(request.username, out message))
            {
                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.InvalidRequest, message = message }
                };
            }

            if (!GetBalanceForSlotBet(request.username, out decimal beforeBalance, out message))
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

            if (!InsertBetForSlotBet(request, tpCustInfo, tpCurrency,
                out bool isDuplicateRound, out decimal afterBalance, out message))
            {
                if (isDuplicateRound)
                {
                    return new AMBSlotBetResponse()
                    {
                        status = new AMBSlotStatusModel() { code = AMBErrorCode.DuplicateRound, message = message }
                    };
                }

                return new AMBSlotBetResponse()
                {
                    status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
                };
            }

            return new AMBSlotBetResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = afterBalance, lastUpdate = getCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = beforeBalance, after = afterBalance },
                    refId = request.transactionId
                }
            };
        }

        public async Task<AMBSlotPayoutResponse> SlotPayout(AMBSlotPayoutRequest request)
        {
            var message = string.Empty;

            return new AMBSlotPayoutResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = 0, lastUpdate = getCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = 0, after = 0 },
                    refId = request.transactionId
                }
            };
        }

        public async Task<AMBSlotCancelResponse> SlotCancel(AMBSlotCancelRequest request)
        {
            var message = string.Empty;

            return new AMBSlotCancelResponse()
            {
                status = new AMBSlotStatusModel() { code = AMBErrorCode.Success, message = message },
                data = new AMBSlotDataModelForBetPayoutCancel()
                {
                    username = request.username,
                    wallet = new AMBSlotWalletModel() { balance = 0, lastUpdate = getCurrentGameTime() },
                    balance = new AMBSlotBalanceModel() { before = 0, after = 0 },
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

            if (string.IsNullOrWhiteSpace(request.agent))
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

        private bool CheckAgentIdForSlotGetBalance(string agentId, out string message)
        {
            message = string.Empty;
            try
            {
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);

                if (thirdPartySetting == null)
                {
                    message = "tpinfo not found.";
                    _logger.LogError($"CheckAgentId Fail:{message}");
                    return false;
                }

                string slotAgent = thirdPartySetting.TpSetting["SlotAgent"];

                if (string.IsNullOrWhiteSpace(slotAgent))
                {
                    message = "tpinfo.agent is null.";
                    _logger.LogError($"CheckAgentId Fail:{message}");
                    return false;
                }

                if (slotAgent != agentId)
                {
                    message = "tpinfo.agent is not equal request.agentId.";
                    _logger.LogError($"CheckAgentId Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError($"CheckTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool CheckTpCustInfoForSlotGetBalance(string username, out string message)
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
                _logger.LogError($"CheckTpCustInfo Exception:{ex.ToString()}");
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
                _logger.LogError($"GetBalance Exception:{ex.ToString()}");
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

            if (string.IsNullOrWhiteSpace(request.agent))
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

        private bool CheckAgentIdForSlotBet(string agentId, out string message)
        {
            message = string.Empty;
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);

            if (thirdPartySetting == null)
            {
                message = "tpinfo not found.";
                _logger.LogError($"CheckAgentId Fail:{message}");
                return false;
            }

            string slotAgent = thirdPartySetting.TpSetting["SlotAgent"];

            if (string.IsNullOrWhiteSpace(slotAgent))
            {
                message = "tpinfo.agent is null.";
                _logger.LogError($"CheckAgentId Fail:{message}");
                return false;
            }

            if (slotAgent != agentId)
            {
                message = "tpinfo.agent is not equal request.agentId.";
                _logger.LogError($"CheckAgentId Fail:{message}");
                return false;
            }

            return true;
        }

        private bool CheckTpCustInfoForSlotBet(string username, out string message)
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
                _logger.LogError($"CheckTpCustInfo Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetBalanceForSlotBet(string username, out decimal beforeBalance, out string message)
        {
            message = string.Empty;
            beforeBalance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username, (int)GameType);

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
                _logger.LogError($"GetBalance Exception:{ex.ToString()}");
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
                _logger.LogError($"GetTpCustInfo Exception:{ex.ToString()}");
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
                _logger.LogError($"GetTpCurrency Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool InsertBetForSlotBet(AMBSlotBetRequest request, TpCustInfo tpCustInfo, TpCurrency tpCurrency,
            out bool isDuplicateRound, out decimal afterBalance, out string message)
        {
            message = string.Empty;
            isDuplicateRound = false;
            afterBalance = 0;
            var bonusId = 0;

            if (tpCurrency.ExchangeRate > 0)
            {
                request.amount = request.amount * tpCurrency.ExchangeRate;
            }

            var data = new BettingDataRNG_AMB()
            {
                username = request.username,
                agent = request.agent,
                game = request.game,
                gameId = request.gameId,
                roundId = request.roundId,
                amount = request.amount,
                currency = request.currency,
                transactionId = request.transactionId,
                timestamp = request.timestamp,
                featureBuy = request.featureBuy,
                isEndRound = request.isEndRound,
                Type = 1, //可能可以不用？
                dtevent = DateTimeOffset.Parse(request.timestamp).DateTime,
            };

            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var result = _bettingService.PlaceBet_AMB(tpCustInfo, data, out decimal afterAmount, out int errorCode, ref bonusId, conn, trans);

                if (!result)
                {
                    if (errorCode == (int)AMBErrorCode.DuplicateRound)
                    {
                        message = "insert bet fail because of DuplicateRound.";
                        _logger.LogError($"InsertBet Fail:{message}");
                        return false;
                    }

                    message = "insert bet fail.";
                    _logger.LogError($"InsertBet Fail:{message}");
                    return false;
                }

                if (afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount = afterAmount / tpCurrency.ExchangeRate;
                    data.amount = data.amount / tpCurrency.ExchangeRate;
                }
                return result;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError($"InsertBet Exception:{ex.ToString()}");
                return false;
            }
        }

        #endregion

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }

        #endregion

    }
}
