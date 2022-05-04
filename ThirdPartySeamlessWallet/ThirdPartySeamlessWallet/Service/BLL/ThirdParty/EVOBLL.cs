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

        public PromoData_EVO redoRequest;

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

        }

        #endregion

        #region Methods

        public async Task<StandardResponse> PromoPayout(PromoData_EVO request, int tpId, int tpGameType, string authToken)
        {
            bool isException = false;
            decimal balance = 0;
            redoRequest = request;

            //if (!CheckRequest(request, out EVOStatus status))
            //{
            //    return new StandardResponse()
            //    {
            //        status = status,
            //        uuid = request.uuid,
            //        retransmission = false
            //    };
            //}

            //if (!CheckSidToken(request, out int tpId, out int tpGameType, out EVOStatus sidStatus))
            //{
            //    return new StandardResponse()
            //    {
            //        status = sidStatus,
            //        retransmission = false
            //    };
            //}

            //if (!CheckAuthToken(authToken, tpId))
            //{
            //    return new StandardResponse()
            //    {
            //        status = EVOStatus.UNKNOWN_ERROR,
            //        retransmission = false
            //    };
            //}

            if (!GetTpCustInfo(request.userId, tpId, out TpCustInfo tpCustInfo))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            if (!GetTpCurrency(tpCustInfo.CurrencyId, tpId, out TpCurrency tpCurrency))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            var data = new Reward_EVO()
            {
                userId = request.userId,
                currency = request.currency,
                game = (request.game == null) ? null : JsonConvert.SerializeObject(request.game),
                promoTransaction = JsonConvert.SerializeObject(request.promoTransaction),
                uuid = request.uuid,
                GameId = request.game?.id,
                GameType = request.game?.type,
                TableId = request.game?.details?.table?.id,
                TableVId = request.game?.details?.table?.vid,
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

            if (!GetExistReward(data, out IEnumerable<Reward_EVO> existDatas))
            {
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            if (existDatas.Any())
            {
                //已存在指定有效派彩
                _logger.LogError($"CheckPayout Fail: already exist valid promo payout by transactionId and roundId.");
                if (!GetBalance(request.userId, tpId, tpGameType, out balance))
                {
                    return new StandardResponse()
                    {
                        status = EVOStatus.UNKNOWN_ERROR.ToString(),
                        uuid = request.uuid,
                        retransmission = false
                    };
                }

                return new StandardResponse()
                {
                    status = EVOStatus.BET_ALREADY_SETTLED.ToString(),
                    uuid = request.uuid,
                    balance = balance,
                    bonus = 0,
                    retransmission = false
                };
            }

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!PlaceReward(data, trans))
            {
                trans.Rollback();
                return new StandardResponse()
                {
                    status = EVOStatus.UNKNOWN_ERROR.ToString(),
                    uuid = request.uuid,
                    retransmission = false
                };
            }

            if (data.PromoTransactionAmount != 0)
            {


                if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out balance, out int bonusId, conn, trans))
                {
                    trans.Rollback();
                    return new StandardResponse()
                    {
                        status = EVOStatus.UNKNOWN_ERROR.ToString(),
                        uuid = request.uuid,
                        retransmission = false
                    };
                }
            }
            else
            {
                if (!GetBalance(request.userId, tpId, tpGameType, out balance))
                {
                    return new StandardResponse()
                    {
                        status = EVOStatus.UNKNOWN_ERROR.ToString(),
                        uuid = request.uuid,
                        retransmission = false
                    };
                }
            }

            trans.Commit();

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
                status = EVOStatus.OK.ToString(),
                uuid = request.uuid,
                balance = balance,
                bonus = 0,
                retransmission = false
            };

        }

        #endregion

        #region Private Methods

        //private bool CheckRequest(PromoData_EVO request, out EVOStatus status)
        //{
        //    status = EVOStatus.OK;

        //    if (request == null)
        //    {
        //        _logger.LogError($"CheckRequest Fail: request is null.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.sid))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.sid is Invalid.");
        //        status = EVOStatus.INVALID_SID;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.userId))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.userId is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.currency))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.currency is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.uuid))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.uuid is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (request.promoTransaction == null)
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.promoTransaction is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.promoTransaction.id))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.promoTransaction.id is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (string.IsNullOrWhiteSpace(request.promoTransaction.type))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.promoTransaction.type is Invalid.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    if (!promoTransactionTypeList.Contains(request.promoTransaction.type))
        //    {
        //        _logger.LogError($"CheckRequest Fail: request.promoTransaction.type can't found.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    return true;
        //}

        ////有重複，要搬到controller
        //private bool CheckSidToken(PromoData_EVO request, out int tpId, out int gameType, out EVOStatus status)
        //{
        //    status = EVOStatus.OK;

        //    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(request.sid));
        //    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);

        //    string tpUserName = sessionKeyArr[2];
        //    gameType = Convert.ToInt32(sessionKeyArr[3]);
        //    tpId = Convert.ToInt32(sessionKeyArr[4]);

        //    if (tpId == 0 || gameType == 0)
        //    {
        //        _logger.LogError($"CheckSidToken Fail: sid.tpId or sid.gametype is Invalid. sid.tp={tpId}. sid.gametype={gameType}.");
        //        status = EVOStatus.INVALID_SID;
        //        return false;
        //    }

        //    if (tpUserName != request.userId)
        //    {
        //        _logger.LogError($"CheckSidToken Fail: sid.tpUserName is equal request.userId.");
        //        status = EVOStatus.INVALID_PARAMETER;
        //        return false;
        //    }

        //    return true;
        //}

        ////有重複，要搬到controller
        //private bool CheckAuthToken(string authToken, int tpId)
        //{
        //    try
        //    {
        //        var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)tpId);

        //        if (thirdPartySetting == null)
        //        {
        //            _logger.LogError($"CheckAuthToken Fail: tpinfo not found.");
        //            return false;
        //        }

        //        string tpAuthToken = thirdPartySetting.TpSetting["AuthToken"];

        //        if (string.IsNullOrWhiteSpace(tpAuthToken))
        //        {
        //            _logger.LogError($"CheckAuthToken Fail: tpinfo.AuthToken is null.");
        //            return false;
        //        }

        //        if (HttpUtility.UrlDecode(authToken) != tpAuthToken)
        //        {
        //            _logger.LogError($"CheckAuthToken Fail: tpinfo.AuthToken is not equal AuthToken.");
        //            return false;
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"CheckAuthToken Exception:{ex.ToString()}");
        //        return false;
        //    }
        //}

        private bool GetTpCustInfo(string username, int tpId, out TpCustInfo tpCustInfo)
        {
            tpCustInfo = null;
            try
            {
                tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, username);
                if (tpCustInfo == null)
                {
                    _logger.LogError($"GetTpCustInfo Fail,");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetTpCustInfo Exception:{ex.Message}");
                Redo(ex);
                return false;
            }
        }

        private bool GetTpCurrency(int currencyId, int tpId, out TpCurrency tpCurrency)
        {
            tpCurrency = null;
            try
            {
                tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, currencyId);
                if (tpCurrency == null)
                {
                    _logger.LogError($"GetTpCurrency Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetTpCurrency Exception:{ex.Message}");
                Redo(ex);
                return false;
            }
        }

        private bool GetExistReward(Reward_EVO data, out IEnumerable<Reward_EVO> existData)
        {
            existData = null;
            try
            {
                existData = _bettingService.IsRewardExist_EVO(data);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetExistReward Exception:{ex.Message}");
                Redo(ex);
                return false;
            }
        }

        private bool PlaceReward(Reward_EVO data, MySqlTransaction trans)
        {
            //存放TP Request的資料，跟匯率無關
            try
            {
                var result = _settledService.PromoPayout_EVO(data, trans);

                if (!result)
                {
                    _logger.LogError($"PlaceReward Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"PlaceReward Exception:{ex.Message}");
                Redo(ex);
                return false;
            }
        }

        private bool SetBalance(TpCustInfo tpCustInfo, Reward_EVO data, decimal rate,
            out decimal afterAmount, out int bonusId,
            MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            bonusId = 0;
            try
            {
                data.PromoTransactionAmount = (rate > 0) ? data.PromoTransactionAmount * rate : data.PromoTransactionAmount;
                //SetPromoPayout並不會用到jacpot的amount，不用特別判斷去轉匯率

                var tpCategory = data.TpId == (int)Tp.EVO ? TpCategory.LiveCasino : TpCategory.Slots;

                var result = _settledService.SetPromoPayout_EVO(tpCustInfo, data,
                    tpCategory, TransType.Prize, $"Prize: {data.PromoTransactionType} ",
                    out afterAmount, ref bonusId, conn, trans);

                if (!result)
                {
                    _logger.LogError($"SetBalance Fail.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SetBalance Exception:{ex.Message}");
                Redo(ex);
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

        private bool GetBalance(string username, int tpId, int tpGameType, out decimal beforeBalance)
        {
            beforeBalance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalance(tpId, username, tpGameType);

                if (result == null)
                {
                    _logger.LogError($"GetBalance Fail: not found.");
                    return false;
                }

                beforeBalance = result.Balance;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"GetBalance Exception:{ex.Message}");
                Redo(ex);
                return false;
            }
        }

        private void Redo(Exception ex)
        {
            _tpRequestFailBLL.SetRequestFail(RequestFailureType.Tp, Tp.EVO, TpRequestFailureCategory.PromoPayout, JsonConvert.SerializeObject(redoRequest), ex.Message);
        }

        #endregion

    }
}
