using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class JokerBLL : IJokerBLL
    {
        #region Fields

        public Tp ThirdParty { get; set; }

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

            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _logger = provider.GetService<ILogger<JokerBLL>>();

            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();

            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        public async Task<JokerResponse> JoinTournament(Dictionary<string, string> request)
        {
            var message = string.Empty;
            var serializeRequest = JsonConvert.DeserializeObject<JoinAndWinRequest>(JsonConvert.SerializeObject(request));

            if (!CheckSignature(serializeRequest.appid, serializeRequest.hash, request))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid Signature"
                };
            }

            if (!CheckRequestForJoinTournament(serializeRequest, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!CheckThirdPartyForJoinTournament(serializeRequest.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCustInfoForJoinTournament(serializeRequest.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = serializeRequest.appid,
                hash = serializeRequest.hash,
                id = serializeRequest.id,
                tournamentid = serializeRequest.tournamentid,
                amount = serializeRequest.amount,
                username = serializeRequest.username,
                extendedinfo = serializeRequest.extendedinfo,
                timestamp = serializeRequest.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromJoin,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related
            };

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!CheckDataForJoinTournament(data, out message))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Success,
                    Message = message
                };
            }

            if (!PlaceJoinForJoinTournament(data, out message, trans))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            trans.Commit();

            if (!GetBalanceForJoinTournament(serializeRequest.username, out decimal balance, out message))
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
                Balance = balance
            };
        }

        public async Task<JokerResponse> WinTournament(Dictionary<string, string> request)
        {
            var message = string.Empty;
            var serializeRequest = JsonConvert.DeserializeObject<JoinAndWinRequest>(JsonConvert.SerializeObject(request));

            if (!CheckSignature(serializeRequest.appid, serializeRequest.hash, request))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid Signature"
                };
            }

            if (!CheckRequestForWinTournament(serializeRequest, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!CheckThirdPartyForWinTournament(serializeRequest.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            if (!GetTpCustInfoForWinTournament(serializeRequest.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = serializeRequest.appid,
                hash = serializeRequest.hash,
                id = serializeRequest.id,
                tournamentid = serializeRequest.tournamentid,
                amount = serializeRequest.amount,
                username = serializeRequest.username,
                extendedinfo = serializeRequest.extendedinfo,
                timestamp = serializeRequest.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromWin,
                TicketType = BettransType.Bonus,
                TicketStatus = TicketStatus.Related
            };

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!CheckDataForWinTournament(data, out message))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Success,
                    Message = message
                };
            }

            if (!PlaceWinForWinTournament(data, out message, trans))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            trans.Commit();

            if (!GetBalanceForWinTournament(serializeRequest.username, out decimal balance, out message))
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
                Balance = balance
            };
        }

        public async Task<JokerResponse> CancelJoinTournament(Dictionary<string, string> request)
        {
            var message = string.Empty;
            var serializeRequest = JsonConvert.DeserializeObject<CancelRequest>(JsonConvert.SerializeObject(request));

            if (!CheckSignature(serializeRequest.appid, serializeRequest.hash, request))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidSignature,
                    Message = "Invalid Signature"
                };
            }

            if (!CheckRequestForCancelJoinTournament(serializeRequest, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidParameters,
                    Message = message
                };
            }

            if (!GetTpCustInfoForCancelJoinTournament(serializeRequest.username, out message))
            {
                return new JokerResponse()
                {
                    Status = JokerErrorCode.InvalidUserNameOrPassword,
                    Message = message
                };
            }

            var data = new RewardRNG_Joker()
            {
                appid = serializeRequest.appid,
                hash = serializeRequest.hash,
                id = serializeRequest.id,
                tournamentid = serializeRequest.jointournamentid,
                username = serializeRequest.username,
                extendedinfo = serializeRequest.extendedinfo,
                timestamp = serializeRequest.timestamp,
                FromAPIType = RewardRNG_JokerTypeEnum.FromJoin,
                TicketType = BettransType.CancelBet,
                TicketStatus = TicketStatus.Refund
            };

            await using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            if (!CheckDataForCancelJoinTournament(data, out int sq, out message))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.AlreadyProcessed,
                    Message = message
                };
            }

            if (!CancelJoinForCancelJoinTournament(sq, out message, trans))
            {
                trans.Rollback();
                return new JokerResponse()
                {
                    Status = JokerErrorCode.Other,
                    Message = message
                };
            }

            trans.Commit();

            if (!GetBalanceForCancelJoinTournament(serializeRequest.username, out decimal balance, out message))
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
                Balance = balance
            };
        }

        #region Private Methods

        private bool CheckSignature(string appid, string hash, Dictionary<string, string> data)
        {
            var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)Tp.Joker);
            var secret = "";
            foreach (var setting in thirdPartySetting.TpSetting)
            {
                if (setting.Value == appid)
                {
                    secret = thirdPartySetting.TpSetting[$"Secret_{setting.Key.Substring(6)}"];
                    break;
                }
            }

            data.Remove("hash");
            var rawData = $"{string.Join("&", data.OrderBy(kp => kp.Key).Select(kp => $"{kp.Key}={kp.Value}"))}{secret}";

            return GetMD5Signature(rawData) == hash;
        }

        private string GetMD5Signature(string rawData)
        {
            using MD5 md5Hash = MD5.Create();
            string md5Content = GetMd5Hash(md5Hash, rawData);
            return md5Content;
        }

        private string GetMd5Hash(MD5 md5Hash, string rawData)
        {
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        #region Join Steps

        private bool CheckRequestForJoinTournament(JoinAndWinRequest request, out string message)
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

            if (string.IsNullOrWhiteSpace(request.tournamentid))
            {
                message = "request.tournamentid is Invalid.";
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

            return true;
        }
        private bool CheckThirdPartyForJoinTournament(string username, out string message)
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
                _logger.LogError(ex, $"CheckThirdParty Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool GetTpCustInfoForJoinTournament(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
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
        private bool CheckDataForJoinTournament(RewardRNG_Joker data, out string message)
        {
            message = string.Empty;
            try
            {
                var slotDatas = _bettingService.IsExistReward_Joker(false, data);

                if (slotDatas.Any())
                {
                    //已存在指定有效投注
                    message = "already exist valid join by username and id.";
                    _logger.LogError($"CheckData Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckData Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool PlaceJoinForJoinTournament(RewardRNG_Joker data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _bettingService.PlaceReward_Joker(data, trans);

                if (!result)
                {
                    message = "place join fail.";
                    _logger.LogError($"PlaceJoin Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceJoin Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool GetBalanceForJoinTournament(string username, out decimal balance, out string message)
        {
            message = string.Empty;
            balance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username);

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

        #region Join Steps

        private bool CheckRequestForWinTournament(JoinAndWinRequest request, out string message)
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

            if (string.IsNullOrWhiteSpace(request.tournamentid))
            {
                message = "request.tournamentid is Invalid.";
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

            return true;
        }
        private bool CheckThirdPartyForWinTournament(string username, out string message)
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
                _logger.LogError(ex, $"CheckThirdParty Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool GetTpCustInfoForWinTournament(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
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
        private bool CheckDataForWinTournament(RewardRNG_Joker data, out string message)
        {
            message = string.Empty;
            try
            {
                var slotDatas = _bettingService.IsExistReward_Joker(false, data);

                if (slotDatas.Any())
                {
                    //已存在指定有效投注
                    message = "already exist valid win by username and id.";
                    _logger.LogError($"CheckData Fail:{message}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckData Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool PlaceWinForWinTournament(RewardRNG_Joker data, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _bettingService.PlaceReward_Joker(data, trans);

                if (!result)
                {
                    message = "place join fail.";
                    _logger.LogError($"PlaceWin Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"PlaceWin Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool GetBalanceForWinTournament(string username, out decimal balance, out string message)
        {
            message = string.Empty;
            balance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username);

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

        #region Cancel Join Steps

        private bool CheckRequestForCancelJoinTournament(CancelRequest request, out string message)
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

            if (string.IsNullOrWhiteSpace(request.jointournamentid))
            {
                message = "request.jointournamentid is Invalid.";
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

            return true;
        }
        private bool GetTpCustInfoForCancelJoinTournament(string username, out string message)
        {
            message = string.Empty;
            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
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
        private bool CheckDataForCancelJoinTournament(RewardRNG_Joker data, out int sq, out string message)
        {
            message = string.Empty;
            sq = 0;
            try
            {
                var joinDatas = _bettingService.IsExistReward_Joker(true, data);

                if (!joinDatas.Any())
                {
                    //不存在指定有效資料
                    message = "not exist join by username and id.";
                    _logger.LogError($"CheckData Fail:{message}");
                    return false;
                }

                if (joinDatas.Count() > 1)
                {
                    //存在多筆指定有效資料
                    message = "already exist multi join by username and id.";
                    _logger.LogError($"CheckData Fail:{message}");
                    return false;
                }

                var joindata = joinDatas.First();

                if (joindata.TicketType == BettransType.CancelBet && joindata.TicketStatus == TicketStatus.Refund)
                {
                    //該筆資料已被取消
                    message = "already cancel assign join by username and id.";
                    _logger.LogError($"CheckData Fail:{message}");
                    return false;
                }

                sq = joinDatas.First().SQ;
                return true;

            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CheckData Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool CancelJoinForCancelJoinTournament(int sq, out string message, MySqlTransaction trans)
        {
            message = string.Empty;
            try
            {
                var result = _settledService.CancelReward_Joker(sq, trans);

                if (!result)
                {
                    message = "CancelJoin fail.";
                    _logger.LogError($"CancelJoin Fail:{message}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                message = "exception happen.";
                _logger.LogError(ex, $"CancelJoin Exception:{ex.ToString()}");
                return false;
            }
        }
        private bool GetBalanceForCancelJoinTournament(string username, out decimal balance, out string message)
        {
            message = string.Empty;
            balance = 0;
            try
            {
                var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username);

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

        #endregion
    }
}
