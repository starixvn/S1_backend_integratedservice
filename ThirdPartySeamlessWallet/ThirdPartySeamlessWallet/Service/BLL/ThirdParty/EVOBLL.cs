using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class EVOBLL : IEVOBLL
    {
        #region Fields

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<EVOBLL> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;

        #endregion

        #region Constructors

        public EVOBLL(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<EVOBLL>>();

            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        #region Methods

        public async Task<StandardResponse> PromoPayout(string requestContent, string authToken)
        {
            var message = string.Empty;
            decimal afterBalance = 0;

            if (string.IsNullOrWhiteSpace(requestContent))
            {
                message = $"Request.Body is Empty.Request.Body={requestContent}.AuthToken={authToken}.";
                return new StandardResponse()
                {
                    status = EVOStatus.INVALID_PARAMETER,
                    uuid = message,
                    retransmission = false
                };
            }

            PromoData_EVO deserializeRequest = JsonConvert.DeserializeObject<PromoData_EVO>(requestContent);

            if (!CheckRequestForPromoPayout(deserializeRequest, out EVOStatus status, out message))
            {
                return new StandardResponse()
                {
                    status = status,
                    uuid = message,
                    retransmission = false
                };
            }


            return new StandardResponse()
            {
                status = EVOStatus.ACCOUNT_LOCKED,
                uuid = requestContent,
                retransmission = false
            };
        }

        #endregion

        #region Promo Payout Step

        private bool CheckRequestForPromoPayout(PromoData_EVO request, out EVOStatus status, out string message)
        {
            message = string.Empty;
            status = EVOStatus.OK;

            if (request == null)
            {
                message = "request is null.";
                _logger.LogError($"CheckRequest Fail:{message}");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.sid))
            {
                message = "request.sid is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                status = EVOStatus.INVALID_SID;
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.userId))
            {
                message = "request.userId is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            return true;
        }

        //private bool CheckAgentIdForPromoPayout(string agentId, out string message)
        //{
        //    message = string.Empty;
        //    try
        //    {
        //        var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);

        //        if (thirdPartySetting == null)
        //        {
        //            message = "tpinfo not found.";
        //            _logger.LogError($"CheckAgentId Fail:{message}");
        //            return false;
        //        }

        //        string slotAgent = thirdPartySetting.TpSetting["SlotAgent"];

        //        if (string.IsNullOrWhiteSpace(slotAgent))
        //        {
        //            message = "tpinfo.agent is null.";
        //            _logger.LogError($"CheckAgentId Fail:{message}");
        //            return false;
        //        }

        //        if (slotAgent != agentId)
        //        {
        //            message = "tpinfo.agent is not equal request.agentId.";
        //            _logger.LogError($"CheckAgentId Fail:{message}");
        //            return false;
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = "exception happen.";
        //        _logger.LogError(ex, $"CheckAgentId Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

        //private bool CheckThirdPartyForPromoPayout(string username, out string message)
        //{
        //    message = string.Empty;
        //    try
        //    {
        //        var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

        //        if (result == null)
        //        {
        //            message = "player not found.";
        //            _logger.LogError($"CheckTpCustInfo Fail:{message}");
        //            return false;
        //        }

        //        if (result.Locked)
        //        {
        //            message = "player is Locked.";
        //            _logger.LogError($"CheckTpCustInfo Fail:{message}");
        //            return false;
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = "exception happen.";
        //        _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

        //private bool GetBalanceForPromoPayout(string username, out decimal beforeBalance, out string message)
        //{
        //    message = string.Empty;
        //    beforeBalance = 0;
        //    try
        //    {
        //        var result = _tpCustInfoBLL.GetBalance((int)ThirdParty, username, (int)GameType);

        //        if (result == null)
        //        {
        //            message = "balance not found.";
        //            _logger.LogError($"GetBalance Fail:{message}");
        //            return false;
        //        }

        //        beforeBalance = result.Balance;
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = "exception happen.";
        //        _logger.LogError(ex, $"GetBalance Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

        private bool GetTpCustInfoForPromoPayout(string username, out TpCustInfo tpCustInfo, out string message)
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

        //private bool GetTpCurrencyForPromoPayout(int currencyId, out TpCurrency tpCurrency, out string message)
        //{
        //    message = string.Empty;
        //    tpCurrency = null;
        //    try
        //    {
        //        tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, currencyId);
        //        if (tpCurrency == null)
        //        {
        //            message = "tpCurrency is null.";
        //            _logger.LogError($"GetTpCurrency Fail:{message}");
        //            return false;
        //        }
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = "exception happen.";
        //        _logger.LogError(ex, $"GetTpCurrency Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

        private bool CheckPayoutForPromoPayout(DateTime partitionTime, BettingDataRNG_AMB data, out string message)
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

        private bool PlacePayoutForPromoPayout(DateTime partitionTime, BettingDataRNG_AMB data, out string message, MySqlTransaction trans)
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

        private bool SetBalanceForPromoPayout(TpCustInfo tpCustInfo, BettingDataRNG_AMB data,
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
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"SetBalance Exception:{ex.ToString()}");
                return false;
            }
        }

        //private bool CheckSettleForPromoPayout(string gameId, out bool doSettle, out string message)
        //{
        //    message = string.Empty;
        //    doSettle = false;
        //    try
        //    {
        //        doSettle = _thirdPartyBLL.GetGameCasinoWithCache((int)ThirdParty).Any(x => x.TypeId == 3 && x.GameId == gameId);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        message = "exception happen.";
        //        _logger.LogError(ex, $" CheckSettle Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

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

    }
}
