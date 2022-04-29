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
using System.Web;
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
        private readonly TpRequestFailBLL _tpRequestFailBLL;
        private readonly ILogger<EVOBLL> _logger;

        private readonly MySqlClient _mainDbClient;

        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;

        public string[] promoTransactionTypeList { get; set; }


        #endregion

        #region Constructors

        public EVOBLL(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpRequestFailBLL = provider.GetService<TpRequestFailBLL>();
            _logger = provider.GetService<ILogger<EVOBLL>>();

            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            promoTransactionTypeList = new string[] { "JackpotWin", "FreeRoundPlayableSpent", "RewardGamePlayableSpent", "RewardGameWinCapReached", "RewardGameMinBetLimitReached", "FromGame", "RtrMonetaryReward" };
        }

        #endregion

        #region Methods

        public async Task<StandardResponse> PromoPayout(PromoData_EVO request, string authToken)
        {
            var message = string.Empty;
            decimal afterBalance = 0;

            if (request == null || string.IsNullOrWhiteSpace(authToken))
            {
                message = $"Request or AuthToken is Empty.Request.Body={JsonConvert.SerializeObject(request)}.AuthToken={authToken}.";
                return new StandardResponse()
                {
                    status = EVOStatus.INVALID_PARAMETER,
                    retransmission = false
                };
            }

            if (!CheckRequestForPromoPayout(request, out EVOStatus status, out message))
            {
                return new StandardResponse()
                {
                    status = status,
                    retransmission = false
                };
            }

            if (!CheckSidTokenForPromoPayout(request, out int tpId, out int tpGameType, out EVOStatus sidStatus, out message))
            {
                return new StandardResponse()
                {
                    status = sidStatus,
                    retransmission = false
                };
            }

            if (!CheckAuthTokenForPromoPayout(authToken, tpId, out message))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR,
                    retransmission = false
                };
            }

            if (!GetTpCustInfoForPromoPayout(request.userId, tpId, out TpCustInfo tpCustInfo, out message))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR,
                    retransmission = false
                };
            }

            if (!GetTpCurrencyForPromoPayout(tpCustInfo.CurrencyId, tpId, out TpCurrency tpCurrency, out message))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR,
                    retransmission = false
                };
            }

            var data = new Rewaard_EVO()
            {
                sid = request.sid,
                userId = request.userId,
                currency = request.currency,
                game = JsonConvert.SerializeObject(request.game),
                //promotionTransaction = JsonConvert.SerializeObject(deserializeRequest.promoTransaction),  //可能會有匯率換算，先不塞值
                uuid = request.uuid,
                GameId = request.game.id,
                GameType = request.game.type,
                TableId = request.game.details.table.id,
                TableVId = request.game.details.table.vid,
                PromoTransactionType = request.promoTransaction.type,
                PromoTransactionId = request.promoTransaction.id,
                PromoTransactionAmount = request.promoTransaction.amount,
                PromoTransactionVoucherId = request.promoTransaction.voucherId,
                PromoTransactionRemainingRounds = request.promoTransaction.remainingRounds,
                PromoTransactionPlayableBalance = request.promoTransaction.playableBalance,
                PromoTransactionBonusConfigId = request.promoTransaction.bonusConfigId,
                PromoTransactionRewardId = request.promoTransaction.rewardId,
                TpId = tpId,
                TpGameType = tpGameType,
                PaidFrom = PaidFromEnum.NVC,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related,
                SettleFlag = SettleFlag.EndGame
            };

            if (tpCurrency.ExchangeRate > 0)
            {

                data.PromoTransactionAmount = data.PromoTransactionAmount * tpCurrency.ExchangeRate;
                data.PromoTransactionPlayableBalance = data.PromoTransactionPlayableBalance * tpCurrency.ExchangeRate;

                if (string.Equals(data.PromoTransactionType, "JackpotWin", StringComparison.OrdinalIgnoreCase))
                {
                    _ = request.promoTransaction.jackpots.Select(x =>
                    {
                        x.winAmount = x.winAmount * tpCurrency.ExchangeRate;
                        return x;
                    });
                }
            }

            data.promotionTransaction = JsonConvert.SerializeObject(request.promoTransaction);

            if (!CheckPayoutForPromoPayout(data, out bool isExist, out message))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR,
                    retransmission = false
                };
            }

            if (!isExist)
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                if (!PlacePayoutForPromoPayout(data, out message, trans))
                {
                    trans.Rollback();
                    return new StandardResponse()
                    {
                        status = EVOStatus.UNKNOWN_ERROR,
                        uuid = request.uuid,
                        retransmission = false
                    };
                }

                if (data.PromoTransactionAmount != 0)
                {
                    if (!SetBalanceForPromoPayout(tpCustInfo, data, out message, out afterBalance, out int bonusId, conn, trans))
                    {
                        trans.Rollback();
                        return new StandardResponse()
                        {
                            status = EVOStatus.UNKNOWN_ERROR,
                            uuid = request.uuid,
                            retransmission = false
                        };
                    }
                }
                else
                {
                    if (!GetBalanceForPromoPayout(request.userId, tpId, tpGameType, out afterBalance, out message))
                    {
                        return new StandardResponse()
                        {
                            status = EVOStatus.UNKNOWN_ERROR,
                            uuid = request.uuid,
                            retransmission = false
                        };
                    }
                }

                trans.Commit();
            }
            else
            {
                if (!GetBalanceForPromoPayout(request.userId, tpId, tpGameType, out afterBalance, out message))
                {
                    return new StandardResponse()
                    {
                        status = EVOStatus.UNKNOWN_ERROR,
                        uuid = request.uuid,
                        retransmission = false
                    };
                }

                return new StandardResponse()
                {
                    status = EVOStatus.BET_ALREADY_SETTLED,
                    uuid = request.uuid,
                    balance = afterBalance,
                    bonus = 0,
                    retransmission = false
                };
            }

            //if (!CheckSettleForSlotPayout(data.gameId, out bool doSettle, out message))
            //{
            //    trans.Rollback();
            //    return new AMBSlotPayoutResponse()
            //    {
            //        status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
            //    };
            //}

            //if (doSettle || data.isEndRound)
            //{
            //    if (!InsertUnsettleForSlotPayout(partitionTime, data.roundId, out message, trans))
            //    {
            //        trans.Rollback();
            //        return new AMBSlotPayoutResponse()
            //        {
            //            status = new AMBSlotStatusModel() { code = AMBErrorCode.ServiceNotAvailable, message = message }
            //        };
            //    }
            //}

            return new StandardResponse()
            {
                status = EVOStatus.OK,
                uuid = request.uuid,
                balance = afterBalance,
                bonus = 0,
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

            if (!promoTransactionTypeList.Contains(request.promoTransaction.type))
            {
                message = "request.promoTransaction.Type is Invalid.";
                _logger.LogError($"CheckRequest Fail:{message}");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            return true;
        }

        private bool CheckSidTokenForPromoPayout(PromoData_EVO request, out int tpId, out int gameType, out EVOStatus status, out string message)
        {
            message = string.Empty;
            status = EVOStatus.OK;

            string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(request.sid));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);

            string tpUserName = sessionKeyArr[2];
            gameType = Convert.ToInt32(sessionKeyArr[3]);
            tpId = Convert.ToInt32(sessionKeyArr[4]);

            if (tpId == 0 || gameType == 0)
            {
                message = $"sid.tpId or sid.gametype is Invalid. sid.tp={tpId}. sid.gametype={gameType}.";
                _logger.LogError($"CheckSidToken Fail:{message}");
                status = EVOStatus.INVALID_SID;
                return false;
            }

            if (tpUserName != request.userId)
            {
                message = "sid.tpUserName is equal request.userId.";
                _logger.LogError($"CheckSidToken Fail:{message}");
                status = EVOStatus.INVALID_PARAMETER;
                return false;
            }

            return true;
        }

        private bool CheckAuthTokenForPromoPayout(string authToken, int tpId, out string message)
        {
            message = string.Empty;
            try
            {
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)tpId);

                if (thirdPartySetting == null)
                {
                    message = "tpinfo not found.";
                    _logger.LogError($"CheckAuthToken Fail:{message}");
                    return false;
                }

                string tpAuthToken = thirdPartySetting.TpSetting["AuthToken"];

                if (string.IsNullOrWhiteSpace(tpAuthToken))
                {
                    message = "tpinfo.AuthToken is null.";
                    _logger.LogError($"CheckAuthToken Fail:{message}");
                    return false;
                }

                if (HttpUtility.UrlDecode(authToken) != tpAuthToken)
                {
                    message = "tpinfo.AuthToken is not equal AuthToken.";
                    _logger.LogError($"CheckAuthToken Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckAuthToken Exception:{ex.ToString()}");
                return false;
            }
        }

        private bool GetTpCustInfoForPromoPayout(string username, int tpId, out TpCustInfo tpCustInfo, out string message)
        {
            message = string.Empty;
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, username);
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

        private bool GetTpCurrencyForPromoPayout(int currencyId, int tpId, out TpCurrency tpCurrency, out string message)
        {
            message = string.Empty;
            tpCurrency = null;
            try
            {
                tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, currencyId);
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

        private bool CheckPayoutForPromoPayout(Rewaard_EVO data, out bool isExist, out string message)
        {
            message = string.Empty;
            isExist = false;
            try
            {
                var promoDatas = _bettingService.IsRewardExist_EVO(data);

                if (promoDatas.Any())
                {
                    //已存在指定有效派彩
                    message = "already exist valid promo payout by transactionId and roundId.";
                    _logger.LogError($"CheckPayout Fail:{message}");
                    isExist = true;
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

        private bool PlacePayoutForPromoPayout(Rewaard_EVO data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.PromoPayout_EVO(data, trans);

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

        private bool SetBalanceForPromoPayout(TpCustInfo tpCustInfo, Rewaard_EVO data,
            out string message, out decimal afterAmount, out int bonusId,
            MySqlConnection conn, MySqlTransaction trans)
        {
            message = string.Empty;
            afterAmount = 0;
            bonusId = 0;
            try
            {
                var tpCategory = data.TpId == (int)Tp.EVO ? TpCategory.LiveCasino : TpCategory.Slots;

                var result = _settledService.SetBalance_EVO(tpCustInfo, data,
                    tpCategory, TransType.Prize, $"Prize:{data.PromoTransactionId}-{data.PromoTransactionType}",
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

        private bool GetBalanceForPromoPayout(string username, int tpId, int tpGameType, out decimal beforeBalance, out string message)
        {
            message = string.Empty;
            beforeBalance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalance(tpId, username, tpGameType);

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

        private void Redo()
        {
            _tpRequestFailBLL.SetRequestFail(RequestFailureType.Tp, Tp.EVO, TpRequestFailureCategory.PromoPayout, JsonConvert.SerializeObject(getReq), ex.Message);
        }

        #endregion

    }
}
