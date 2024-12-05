using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Model.TpMerchantBalance.Response;

using ThirdPartyService.Extensions;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.BLL
{
    public class ThirdPartyBLL
    {
        #region Fields

        private readonly ILogger<ThirdPartyBLL> _logger;

        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpTransferInfo tpTransferInfo;

        private readonly TpCustInfoService tpCustInfoService;
        private readonly TpTransferBonusInfoService tpTransferBonusInfoService;
        private readonly TpCustBalanceService _tpCustBalanceService;
        private readonly TpTransferInfo _tpTransferInfo;
        private readonly CacheHelper _cacheHelper;
        private readonly IReadOnlyDictionary<int, TpListOption> _TpList;
        private readonly Dictionary<int, TpGameTypeOption> _tpGameTypeConfig;
        private readonly IReadOnlyDictionary<int, CurrencyOption> _currencyList;
        private readonly MySqlClient _mainDbClient;
        private readonly Common _commonService;

#if DEBUG

        /// <summary>
        /// PT異常客戶清單(測試機)，共13筆
        /// </summary>
        private readonly int[] PTWrongCustList = new int[]
            {1342, 249646, 249688, 249768, 249782, 249784, 249827, 249832, 249835, 249862, 249863, 249864, 249892};

        /// <summary>
        /// 9W異常客戶清單(測試機)，共12筆
        /// </summary>
        private readonly int[] NineWWrongCustList = new int[]
            {565, 602, 1017, 127559, 249667, 249745, 249814, 249874, 249951, 250066, 249951, 250057};

#endif

        #endregion Fields

        #region Constructors

        public ThirdPartyBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<ThirdPartyBLL>>();

            thirdPartyHelper = provider.GetRequiredService<ThirdPartyHelper>();
            tpTransferInfo = provider.GetRequiredService<TpTransferInfo>();

            tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
            tpTransferBonusInfoService = provider.GetRequiredService<TpTransferBonusInfoService>();
            _tpCustBalanceService = provider.GetRequiredService<TpCustBalanceService>();
            _tpTransferInfo = provider.GetRequiredService<TpTransferInfo>();
            _cacheHelper = provider.GetRequiredService<CacheHelper>();
            _commonService = provider.GetRequiredService<Common>();

            ThirdPartyHelperBase.AuthProvider = provider.GetRequiredService<AuthBLL>();
            ThirdPartyHelperBase.ServerInfoProvider = provider.GetRequiredService<ServerInfoBLL>();
            ThirdPartyHelperBase.ContentRootPath = provider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            ThirdPartyHelperBase.TpBetRecordProvider = provider.GetRequiredService<TpBetRecordService>();
            ThirdPartyHelperBase.TpCustInfoProvider = provider.GetRequiredService<TpCustInfoBLL>();
            ThirdPartyHelperBase.BetLimitProvider = provider.GetRequiredService<BetLimitService>();

            var tpGameTypeOptions = provider.GetRequiredService<IEnumerable<TpGameTypeOption>>();
            _tpGameTypeConfig = tpGameTypeOptions.ToDictionary(i => i.Type, i => i);
            _TpList = provider.GetRequiredService<IEnumerable<TpListOption>>().ToDictionary(d => d.Id, d => d);
            _currencyList = provider.GetRequiredService<IEnumerable<CurrencyOption>>().ToDictionary(d => d.CurrencyId, d => d);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion Constructors

        public ApiResult GotoGame(TpInfo tpInfo)
        {
            var apiResult = new ApiResult();
            var tpLangId = thirdPartyHelper.GetTpLanId(tpInfo.TpId, tpInfo.LangId);

            if (string.IsNullOrEmpty(tpLangId))
            {
                apiResult.ErrorCode = ErrorCode.InvalidLanguage;
                apiResult.Message = $"Language Id: {tpInfo.LangId} has not found.";
                return apiResult;
            }

            // free game no need to login
            if (tpInfo.inputs.ContainsKey("fun") &&
                tpInfo.inputs["fun"].Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpInfo.SiteId);
                var inputs =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper
                        .GetThirdPartyBySite(tpInfo.TpId, tpInfo.SiteId).Setting);
                if (tpInfo.inputs != null)
                {
                    foreach (var infoI in tpInfo.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }

                TpCustInfo freeCustInfo = new TpCustInfo();
                freeCustInfo.SiteId = tpInfo.SiteId;
                freeCustInfo.CustId = tpInfo.CustId;
                apiResult = tpHelper.Login(freeCustInfo, (AppType)tpInfo.AppType, tpLangId, inputs);

                return apiResult;
            }

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId, tpInfo.LangId);
            if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
            {
                var tpCustInfo = tpCustInfoResult.Data;
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfo.SiteId);

                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper
                    .GetThirdPartyBySite(tpInfo.TpId, tpCustInfo.SiteId).Setting);
                if (tpInfo.inputs != null)
                {
                    foreach (var infoI in tpInfo.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }

                // Test account only allow login for saba
                // 先讓HL過 後續確定有一家轉帳制 到時候一起處理
                if (tpCustInfoResult.Data.IsTest && _commonService.IsProdEnvironment() && tpInfo.TpId != 1 && tpInfo.TpId != 58)
                {
                    apiResult.ErrorCode = ErrorCode.NoAuthority;
                    apiResult.RawMessage = apiResult.Message = "This Account not allow for access";
                }
                else
                {
                    var tpGameType = tpInfo.inputs.ContainsKey("tpGameType") ? tpInfo.inputs["tpGameType"] : "0";
                    var currencyId = tpCustInfoResult.Data.CurrencyId;
                    var tpBetLimit = thirdPartyHelper.GetTPBetLimit(currencyId, int.Parse(tpGameType));

                    if (tpBetLimit != null && tpBetLimit.DefaultGroup != null)
                    {
                        inputs.Add("betLimitGroup", tpBetLimit.DefaultGroup);
                    }

                    apiResult = tpHelper.Login(tpCustInfoResult.Data, (AppType)tpInfo.AppType, tpLangId, inputs);

                    SentrySdk.CaptureMessage($"xxx {nameof(ThirdPartyBLL)} at {DateTime.Now}" +
                                             $"{Environment.NewLine}-tpCustInfo {JsonConvert.SerializeObject(tpCustInfo)}" +
                                             $"{Environment.NewLine}-tpInfo {JsonConvert.SerializeObject(tpInfo)}" +
                                             $"{Environment.NewLine}-inputs {JsonConvert.SerializeObject(inputs)}" +
                                             $"{Environment.NewLine}-apiResult {JsonConvert.SerializeObject(apiResult)}");
                }

                /*Use Async to Record Log*/
                Task.Run(() =>
                {
                    tpCustInfoService.SetTpLastLoginTime(tpInfo.CustId, tpInfo.TpId);
                    SetRecentlyPlayed(tpInfo);
                    SetRecentlyViewed(tpInfo);
                });
            }
            else
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.Message = tpCustInfoResult.Message;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
            }

            return apiResult;
        }

        public ApiResult<TransferResult> Transfer(TpTransfer tpTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            try
            {
                var result = GetPrefix(tpTransfer, apiResult, out var prefix);
                if (result.ErrorCode == ErrorCode.InvalidParameter || result.ErrorCode == ErrorCode.AutoTransferBonusFail)
                {
                    return result;
                }

                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpTransfer.CustId, tpTransfer.TpId, tpTransfer.LangId);

                // Test account create 3rd party only for saba
                // 先讓HL過 後續確定有一家轉帳制 到時候一起處理
                if (tpCustInfoResult.Data.IsTest && _commonService.IsProdEnvironment() && tpTransfer.TpId != 1 && tpTransfer.TpId != 58)
                {
                    apiResult.ErrorCode = ErrorCode.NoAuthority;
                    apiResult.RawMessage = "This Account is for Testing.";
                    return apiResult;
                }
                else if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
                {
                    apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                    apiResult.Message = tpCustInfoResult.Message;
                    apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                    apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
                    return apiResult;
                }

                if (tpCustInfoResult.Data.IsTest == false && ThirdPartyIsMaintain(tpCustInfoResult.Data.SiteId, tpTransfer.TpId))
                {
                    apiResult.ErrorCode = ErrorCode.ThirdPartyUnderMaintenance;
                    apiResult.Message = "Third party under maintenance.";
                    return apiResult;
                }

                IThirdPartyHelper tpHelper =
                    thirdPartyHelper.GetThirdPartyHelper(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel =
                    thirdPartyHelper.GetThirdPartyBySite(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                if (!string.IsNullOrEmpty(tpTransfer.LangId))
                {
                    inputs.Add("langId", tpTransfer.LangId);
                }

                var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
                if (balanceApiResult.ErrorCode != ErrorCode.Success)
                {
                    apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                    apiResult.Message = balanceApiResult.Message;
                    apiResult.RawErrorCode = balanceApiResult.RawErrorCode; /*3RD Error Code*/
                    apiResult.RawMessage = balanceApiResult.RawMessage; /*3RD Error Message*/
                    return apiResult;
                }

                if (tpTransfer.inputs != null && tpTransfer.inputs.ContainsKey("AllOut"))
                {
                    decimal _Amount = balanceApiResult.Data.balance;
                    if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                    {
                        _Amount *= tpCustInfoResult.Data.ExchangeRate;
                    }

                    tpTransfer.direction = Direction.ToMain;
                    tpTransfer.Amount = Math.Abs(_Amount);
                }

                // int transfer amount
                var to2DecimalAmount = DealTransferAmount(tpTransfer.Amount, tpCustInfoResult.Data.ExchangeRate, tpTransfer.TpId, out decimal credit, out bool execute, out bool skip);
                if (!skip)
                {
                    if (!execute)
                    {
                        apiResult.ErrorCode = ErrorCode.Fail;
                        apiResult.Data.Status = TransferStatus.Fail;
                        apiResult.RawMessage = $"Transfer failed due to decimal greater than 2 digits. ({Math.Abs(credit)})";
                        apiResult.RawErrorCode = ErrorCode.DecimalOutOfRange.ToString();
                        return apiResult;
                    }
                    else
                    {
                        if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                        {
                            to2DecimalAmount *= tpCustInfoResult.Data.ExchangeRate;
                        }
                        tpTransfer.Amount = Math.Abs(to2DecimalAmount);
                    }
                }

                string transId = null;
                var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
                string thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;
                var tpTransferReqResult =
                    tpTransferInfo.TpTransferReqV2(tpTransfer, tpCustInfoResult.Data, thirdId, prefix, null, out transId);
                if (tpTransferReqResult.ErrorCode != ErrorCode.Success)
                {
                    return tpTransferReqResult;
                }

                decimal amount = tpTransfer.Amount;

                if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                {
                    amount = tpTransfer.Amount / tpCustInfoResult.Data.ExchangeRate;
                }

                apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, amount, tpTransfer.direction, transId,
                    thirdId);

                tpTransferInfo.TpTransferCommit(apiResult, tpCustInfoResult.Data, tpTransfer, transId);
                /*Use Async to Record Log*/
                if (apiResult.ErrorCode == ErrorCode.Success)
                {
                    Task.Run(() => { tpCustInfoService.SetTpLastTradingTime(tpTransfer.CustId, tpTransfer.TpId); });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

            return apiResult;
        }

        private ApiResult<TransferResult> GetPrefix(TpTransfer tpTransfer, ApiResult<TransferResult> apiResult, out string prefix)
        {
            prefix = TransferPrefix.T.ToString();

            if ((tpTransfer.direction != Direction.ToMain && tpTransfer.direction != Direction.ToTp) ||
                tpTransfer.Amount <= 0)
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
                return apiResult;
            }

            // source autotransfer running bonus failed
            if (tpTransfer.inputs != null && tpTransfer.inputs.ContainsKey("Source"))
            {
                if (tpTransfer.inputs["Source"] == "Auto")
                {
                    prefix = TransferPrefix.AT.ToString();
                    var custBonus = thirdPartyHelper.CheckBonusWallet(tpTransfer.CustId, tpTransfer.TpId);
                    if (custBonus != null)
                    {
                        apiResult.ErrorCode = ErrorCode.AutoTransferBonusFail;
                        return apiResult;
                    }
                }
                else if (tpTransfer.inputs["Source"] == "BonusCancel")
                {
                    prefix = TransferPrefix.BT.ToString();
                }
            }

            return apiResult;
        }

        public ApiResult<UserBalance> GetBalance(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.SystemError
            };

            try
            {
                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId, tpInfo.LangId);
                if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
                {
                    if (tpCustInfoResult.Data.IsTest == false && ThirdPartyIsMaintain(tpCustInfoResult.Data.SiteId, tpInfo.TpId))
                    {
                        apiResult.ErrorCode = ErrorCode.ThirdPartyUnderMaintenance;
                        apiResult.Message = "Third party under maintenance.";
                        return apiResult;
                    }

                    IThirdPartyHelper tpHelper =
                        thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfoResult.Data.SiteId);

                    var oThirdPartyModel =
                        thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                    var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                    if (!string.IsNullOrEmpty(tpInfo.LangId))
                    {
                        inputs.Add("langId", tpInfo.LangId);
                    }

                    apiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);

                    if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                    {
                        apiResult.Data.balance = apiResult.Data.balance * tpCustInfoResult.Data.ExchangeRate;
                        apiResult.Data.outstanding = apiResult.Data.outstanding * tpCustInfoResult.Data.ExchangeRate;
                    }
                }
                else
                {
                    apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

            return apiResult;
        }

        public ApiResult Logout(TpInfo tpInfo)
        {
            var apiResult = new ApiResult();
            apiResult.ErrorCode = ErrorCode.SystemError;
            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
            if (tpCustInfoResult.Data != null)
            {
                var tpHelper =
                    (IThirdPartyParticular)thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId,
                        tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                apiResult = tpHelper.Logout(tpCustInfoResult.Data, inputs);

                return apiResult;
            }
            else
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
            }

            return apiResult;
        }

        public void CheckTransfer(TpTransferInfoModel tpTransferInfoModel)
        {
            if (tpTransferInfoModel.Direction == (int)Direction.ToTp &&
                tpTransferInfoModel.Status == (int)TransferStatus.Pending)
            {
                bool isSuccess = tpTransferInfo.SetTransferFail(tpTransferInfoModel.TransId);
            }
            else
            {
                //tpTransferInfoModel.CustId
                var tpCustInfoResult =
                    tpCustInfoService.GetTpCustInfo(tpTransferInfoModel.CustId, tpTransferInfoModel.TpId);
                IThirdPartyHelper tpHelper =
                    thirdPartyHelper.GetThirdPartyHelper(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel =
                    thirdPartyHelper.GetThirdPartyBySite(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                decimal amount = tpTransferInfoModel.Amount;
                if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                {
                    amount = tpTransferInfoModel.Amount / tpCustInfoResult.Data.ExchangeRate;
                }

                var apiResult = tpHelper.CheckTransfer(tpCustInfoResult.Data, inputs, amount,
                    (Direction)tpTransferInfoModel.Direction, tpTransferInfoModel.TransId,
                    tpTransferInfoModel.ThirdId);
                var tpTransfer = new TpTransfer
                {
                    direction = (Direction)tpTransferInfoModel.Direction,
                    Amount = tpTransferInfoModel.Amount,
                    TpId = tpTransferInfoModel.TpId
                };

                tpTransferInfo.TpTransferCommit(apiResult, tpCustInfoResult.Data, tpTransfer,
                    tpTransferInfoModel.TransId);

                if (tpTransferInfoModel.TransId.StartsWith(TransferPrefix.BT.ToString())
                    && !string.IsNullOrEmpty(tpTransferInfoModel.ExtensionInfo))
                {
                    tpTransferBonusInfoService.TpTransferBonusCancelCommit(
                        JsonConvert.DeserializeObject<TpBonusCancelTransfer>(tpTransferInfoModel.ExtensionInfo),
                        apiResult);
                }
            }
        }

        public ApiResult BonusTransfer(TpBonusTransfer tpBonusTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            var statusList = new List<int> { (int)TransferStatus.PendingD };
            var checkResult = _tpTransferInfo.TpBonusTransferInfoPendingGet(tpBonusTransfer.CustId,
                JsonConvert.SerializeObject(statusList));

            if (checkResult != null)
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.RawErrorCode = ErrorCode.Fail.ToString();
                apiResult.Message = "Transfer processing";
                apiResult.RawMessage = "Transfer processing";
                return apiResult;
            }

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpBonusTransfer.CustId, tpBonusTransfer.TpId);

            // Test account create 3rd party only for saba
            if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
                return apiResult;
            }

            var tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpBonusTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpBonusTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode; /*3RD Error Code*/
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = balanceApiResult.RawMessage; /*3RD Error Message*/
                return apiResult;
            }

            string transId = null;
            var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
            string thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;
            var tpTransferReqResult = tpTransferBonusInfoService.TpTransferBonusReq(tpBonusTransfer,
                tpCustInfoResult.Data, thirdId, out transId, out int bonusId);
            if (tpTransferReqResult.ErrorCode != ErrorCode.Success)
            {
                return tpTransferReqResult;
            }

            decimal bonus = tpBonusTransfer.Bonus;
            decimal bonusAmount = tpBonusTransfer.BonusAmount;
            decimal lockedAmount = tpBonusTransfer.LockedAmount;
            decimal rolloverAmount = tpBonusTransfer.RolloverAmount;

            if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
            {
                bonus /= tpCustInfoResult.Data.ExchangeRate;
                lockedAmount /= tpCustInfoResult.Data.ExchangeRate;
                rolloverAmount /= tpCustInfoResult.Data.ExchangeRate;
            }

            apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, bonus, direction: Direction.ToTp,
                transId, thirdId); /*tpHelperParticular.BonusTransfer(tpCustInfoResult.Data, inputs, bonusAmount, transId, thirdId);*/

            tpTransferBonusInfoService.TpTransferBonusCommit(apiResult, tpCustInfoResult.Data, tpBonusTransfer, transId, bonusId);

            if (apiResult.ErrorCode == ErrorCode.Success)
            {
                Task.Run(() =>
                {
                    tpCustInfoService.SetTpLastTradingTime(tpBonusTransfer.CustId, tpBonusTransfer.TpId);
                });
            }

            return apiResult;
        }

        public void BonusCheckTransfer(TpPendingBonusTransferInfoModel tpTransferInfoModel)
        {
            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpTransferInfoModel.CustId, tpTransferInfoModel.TpId);
            var tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
            var amount = tpTransferInfoModel.Bonus;

            var apiResult = tpHelper.CheckTransfer(tpCustInfoResult.Data, inputs, amount, Direction.ToTp, tpTransferInfoModel.TransId, tpTransferInfoModel.ThirdId);

            var req = new TpBonusTransfer
            {
                TpId = tpTransferInfoModel.TpId,
                CustId = tpTransferInfoModel.CustId,
                Bonus = tpTransferInfoModel.Bonus,
                BonusAmount = tpTransferInfoModel.BonusAmount,
                LockedAmount = tpTransferInfoModel.LockedAmount,
                RolloverAmount = tpTransferInfoModel.RolloverAmount,
                ExpiryTime = tpTransferInfoModel.ExpiredDate
            };
            tpTransferBonusInfoService.TpTransferBonusCheckCommit(apiResult, tpCustInfoResult.Data, req,
                tpTransferInfoModel);

            if (apiResult.Data.Status == TransferStatus.Fail)
            {
                BonusTransfer(req);
            }
        }

        public ApiResult BonusCancelTransfer(TpBonusCancelTransfer tpBonusCancelTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            var tpTransfer = new TpTransfer
            {
                TpId = tpBonusCancelTransfer.TpId,
                CustId = tpBonusCancelTransfer.CustId,
                direction = Direction.ToMain,
                Amount = tpBonusCancelTransfer.RemainingBonus,
                inputs = new Dictionary<string, string>()
                {
                    { "Source", "BonusCancel" }
                }
            };

            var result = GetPrefix(tpTransfer, apiResult, out var prefix);
            if (result.ErrorCode == ErrorCode.InvalidParameter || result.ErrorCode == ErrorCode.AutoTransferBonusFail)
            {
                return result;
            }

            if (tpTransferInfo.Service_TpTransferInfo_Bonus_Get(tpBonusCancelTransfer.BonusId))
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Message = "Transfer Processing.";
                return apiResult;
            }

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpBonusCancelTransfer.CustId, tpBonusCancelTransfer.TpId);

            if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = tpCustInfoResult.RawMessage;
                return apiResult;
            }

            var tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpBonusCancelTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpBonusCancelTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode;
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = balanceApiResult.RawMessage;
                return apiResult;
            }

            //檢查第三方剩餘金額並判斷是否需要修改 Bonus Amount
            var checkTransferAmount = tpTransfer.Amount;
            var thirdPartyBalance = balanceApiResult.Data.balance;
            if (tpTransfer.TpId == (int)TpEnum.Saba && thirdPartyBalance < 1)
            {
                thirdPartyBalance = 0;
            }
            if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
            {
                thirdPartyBalance *= tpCustInfoResult.Data.ExchangeRate;
                checkTransferAmount /= tpCustInfoResult.Data.ExchangeRate;
            }

            if (tpTransfer.TpId == (int)TpEnum.Saba && checkTransferAmount < 1)
            {
                checkTransferAmount = 0;
            }

            thirdPartyBalance = thirdPartyBalance < 0 ? 0 : thirdPartyBalance;
            if (tpTransfer.Amount > thirdPartyBalance || checkTransferAmount == 0)
            {
                var amount = Math.Min(thirdPartyBalance, checkTransferAmount * tpCustInfoResult.Data.ExchangeRate);
                tpTransfer.Amount = amount;
                tpBonusCancelTransfer.RefundAmount -= tpBonusCancelTransfer.RemainingBonus - amount;
                if (tpBonusCancelTransfer.RefundAmount < 0)
                {
                    tpBonusCancelTransfer.RefundAmount = 0;
                }

                tpBonusCancelTransfer.RemainingBonus = amount;

                _logger.LogInformation($"After Calculate: CustId={tpTransfer.CustId}, BonusId={tpBonusCancelTransfer.BonusId}, " +
                                       $"ThirdPartyBalance={thirdPartyBalance}, CheckTransferAmount={checkTransferAmount}, TransferAmount={tpTransfer.Amount}, " +
                                       $"RemainingBonus={tpBonusCancelTransfer.RemainingBonus}, " +
                                       $"RefundAmount={tpBonusCancelTransfer.RefundAmount}");
            }

            if (tpTransfer.Amount > 0)
            {
                var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
                var thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;
                tpTransfer.BonusId = tpBonusCancelTransfer.BonusId;
                var tpTransferReqResult =
                    tpTransferInfo.TpTransferReqV2(tpTransfer, tpCustInfoResult.Data, thirdId, prefix,
                        tpBonusCancelTransfer, out var transId);
                if (tpTransferReqResult.ErrorCode != ErrorCode.Success)
                {
                    return tpTransferReqResult;
                }

                var amount = tpTransfer.Amount;
                if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                {
                    amount /= tpCustInfoResult.Data.ExchangeRate;
                }

                apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, amount, Direction.ToMain,
                    transId, thirdId);

                tpTransferInfo.TpTransferCommit(apiResult, tpCustInfoResult.Data, tpTransfer, transId);
            }
            else
            {
                apiResult.Data.Status = TransferStatus.Success;
                apiResult.ErrorCode = ErrorCode.Success;
            }

            tpTransferBonusInfoService.TpTransferBonusCancelCommit(tpBonusCancelTransfer, apiResult);

            if (apiResult.ErrorCode == ErrorCode.Success)
            {
                Task.Run(() =>
                {
                    tpCustInfoService.SetTpLastTradingTime(tpBonusCancelTransfer.CustId, tpBonusCancelTransfer.TpId);
                });
            }

            return apiResult;
        }

        public ApiResult FixNegative(TpInfo tpInfo)
        {
            var apiResult = new ApiResult();

            apiResult.ErrorCode = ErrorCode.SystemError;
            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
            if (tpCustInfoResult.Data != null)
            {
                IThirdPartyHelper tpHelper =
                    thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
                var balanceResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
                if (balanceResult.Data.balance < 0)
                {
                    apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, Math.Abs(balanceResult.Data.balance),
                        Direction.ToTp, Guid.NewGuid().ToString("N"), null);
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.TransferFail;
                    apiResult.Message =
                        $"{tpCustInfoResult.Data.TpUserName}'s balance is {balanceResult.Data.balance.ToString("#,##0.##")}, It's not Negative.";
                }
            }

            return apiResult;
        }

        public ApiResult<AdjustWalletResult> AdjustWallet(AdminTpTransfer adminTpTransfer)
        {
            var apiResult = new ApiResult<AdjustWalletResult>();

            if (adminTpTransfer.direction != Direction.ToMain && adminTpTransfer.direction != Direction.ToTp)
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
                return apiResult;
            }
            else if (adminTpTransfer.Amount <= 0)
            {
                apiResult.ErrorCode = ErrorCode.InvalidParameter;
                return apiResult;
            }

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(adminTpTransfer.CustId, adminTpTransfer.TpId);

            if (tpCustInfoResult.Data == null)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
                return apiResult;
            }

            IThirdPartyHelper tpHelper =
                thirdPartyHelper.GetThirdPartyHelper(adminTpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel =
                thirdPartyHelper.GetThirdPartyBySite(adminTpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode; /*3RD Error Code*/
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = apiResult.Message = balanceApiResult.RawMessage; /*3RD Error Message*/
                return apiResult;
            }

            string transId = null;
            var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
            string thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;

            decimal amount = adminTpTransfer.Amount;

            if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
            {
                amount = adminTpTransfer.Amount / tpCustInfoResult.Data.ExchangeRate;
            }

            tpTransferInfo.InsAdjustWalletLog(tpCustInfoResult.Data, adminTpTransfer, out transId);

            if (string.IsNullOrEmpty(transId))
            {
                _logger.LogError($"InsAdjustWalletLog failed.");
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.Message = "AdjustWallet Failed.";

                return apiResult;
            }

            var transferResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, amount, adminTpTransfer.direction, transId,
                thirdId);

            tpTransferInfo.SetAdjustWalletLog(transferResult, transId);
            if (transferResult.ErrorCode == ErrorCode.Success
                && adminTpTransfer.IsDepositId && adminTpTransfer.DepositId > 0) // IsDepositId = true
            {
                var adjustAmount = adminTpTransfer.Amount * (adminTpTransfer.direction == Direction.ToTp ? 1 : -1);
                var reslutCode = tpTransferInfo.SetAdjustmentActualDeposit(transId, adminTpTransfer.DepositId, adjustAmount);

                if (reslutCode != 0)
                {
                    _logger.LogError(
                        $"SetAdjustmentActualDeposit Failed. TransId: {transId}, DepositId:{adminTpTransfer.DepositId}, reslutCode:{reslutCode}");
                }
            }

            apiResult.ErrorCode = transferResult.ErrorCode;
            apiResult.Message = transferResult.Message;
            apiResult.RawErrorCode = transferResult.RawErrorCode;
            apiResult.RawMessage = transferResult.RawMessage;
            apiResult.Data = new AdjustWalletResult(transferResult.Data, transId);

            return apiResult;
        }

        public ApiResult<AutoTransferResult> AutoTransfer(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<AutoTransferResult>();
            apiResult.Data.Guid = Guid.NewGuid().ToString();
            apiResult.Data.FromTpId = 0;
            apiResult.Data.ToTpId = tpInfo.TpId;
            apiResult.Data.CustId = tpInfo.CustId;
            apiResult.Data.MainBalanceBefore = "--";
            apiResult.Data.MainBalanceAfter = "--";
            apiResult.Data.GameBalance = "--";

            // get cust auto transfer status
            var custWallet = tpCustInfoService.GetCustWallet(tpInfo.CustId);
            if (custWallet == null)
            {
                apiResult.ErrorCode = ErrorCode.UserNotExist;
                apiResult.Message = "This Account Cust Wallet Not Found";
                return apiResult;
            }

            apiResult.Data.MainBalanceBefore = custWallet.Balance.ToString();

            if (custWallet.AutoTransfer != (int)AutoType.Auto)
            {
                apiResult.ErrorCode = ErrorCode.AutoTransferIsOff;
                apiResult.Message = "This Account Auto Transfer Is Off";
                return apiResult;
            }

            var msg = $"Main Before : \r\n{JsonConvert.SerializeObject(apiResult)}\r\n";

            apiResult.Data.AutoTransfer = custWallet.AutoTransfer;

            TpTransfer transfer = new TpTransfer();
            var subResult = new ApiResult<AutoTransferResult>();
            subResult = apiResult;

            // get all transfer games and status is on
            var transferGames =
                thirdPartyHelper.GetThirdPartyByWalletTypeByCustId((int)WalletType.Transfer, custWallet.SiteId,
                    tpInfo.CustId);
            if (transferGames.Any())
            {
                int alphaTpId = tpInfo.TpId;
                bool toTransferGame = false;
                Dictionary<string, string> preInput = new Dictionary<string, string>();
                preInput.Add("AllOut", "Y");
                preInput.Add("Source", "Auto");

                // transfer balance to main wallet
                foreach (var item in transferGames)
                {
                    int betaTpId = item.Id;
                    if (betaTpId == alphaTpId)
                    {
                        // 判斷是否為進入該款遊戲 是:轉入該款遊戲
                        toTransferGame = true;
                        //continue;

                        if ((item.WalletMode == (int)WalletMode.Manual) && toTransferGame)
                        {
                            // 判斷是否手動轉帳遊戲 是:跳過不轉入該款遊戲
                            toTransferGame = false;
                        }
                    }

                    if (item.WalletMode == (int)WalletMode.Manual)
                    {
                        msg += $"Skip Manual Transfer Game : {item.Id}\r\n";
                        continue;
                    }

                    var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, betaTpId);
                    if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
                    {
                        // get each games third-party of balance
                        var preTpInfo = tpInfo;
                        preTpInfo.TpId = betaTpId;
                        try
                        {
                            var preBalance = GetBalance(preTpInfo);
                            if (preBalance.ErrorCode == ErrorCode.Success && preBalance.Data.balance > 0)
                            {
                                // prepare transfer param
                                transfer.TpId = preTpInfo.TpId;
                                transfer.Amount = 1; // temporary use 1 for pass validation
                                transfer.direction = Direction.ToMain;
                                transfer.CustId = tpInfo.CustId;
                                transfer.inputs = preInput;

                                // 同步
                                var subTrans = Transfer(transfer);

                                /*
                                // 非同步
                                var taskTransfer = Task.Run(() => Transfer(transfer));
                                taskTransfer.Wait(1000);
                                */
                                subResult.Data.FromTpId = preTpInfo.TpId;
                                subResult.Data.ToTpId = 0;
                                subResult.Data.MainBalanceBefore = "--";
                                subResult.Data.MainBalanceAfter = "--";
                                subResult.Data.GameBalance = (subTrans.ErrorCode == ErrorCode.Success)
                                    ? preBalance.Data.balance.ToString()
                                    : "--";
                                subResult.ErrorCode = subTrans.ErrorCode;
                                subResult.RawErrorCode = subTrans.RawErrorCode ?? "";
                                subResult.RawMessage = subTrans.RawMessage ?? "";
                                msg += $"Game To Main : \r\n{JsonConvert.SerializeObject(subResult)}\r\n";
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                }

                if (toTransferGame)
                {
                    Dictionary<string, string> input = new Dictionary<string, string>();
                    custWallet = tpCustInfoService.GetCustWallet(tpInfo.CustId);
                    transfer.TpId = alphaTpId;
                    transfer.Amount = custWallet.Balance;
                    transfer.direction = Direction.ToTp;
                    transfer.CustId = tpInfo.CustId;
                    input.Add("AllIn", "Y");
                    input.Add("Source", "Auto");
                    transfer.inputs = input;

                    var doTransfer = Transfer(transfer);
                    apiResult.Data.FromTpId = 0;
                    apiResult.Data.ToTpId = tpInfo.TpId;
                    subResult.Data.MainBalanceBefore = "--";
                    apiResult.Data.MainBalanceAfter = (doTransfer.ErrorCode == ErrorCode.Success)
                        ? custWallet.Balance.ToString()
                        : "--";
                    apiResult.Data.GameBalance = "--";
                    apiResult.ErrorCode = doTransfer.ErrorCode;
                    apiResult.Message = doTransfer.Message;
                    apiResult.RawErrorCode = doTransfer.RawErrorCode;
                    apiResult.RawMessage = doTransfer.RawMessage;
                    msg += $"Main After : \r\n{JsonConvert.SerializeObject(apiResult)}\r\n";
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.AutoTransferIsSeamless;
                    apiResult.Message = "Go To Seamless Game Is No Need To Transfer";
                }
                _logger.LogInformation($"AutoTransfer:{msg}");
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.AutoTransferFail;
                apiResult.Message = "Get Transfer Games Failed";
            }

            return apiResult;
        }

        /// <summary>
        /// 取指定條件玩家的第三方餘額
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <param name="siteId">平台ID</param>
        /// <param name="balanceDatas">餘額資料</param>
        public bool GetBalance(bool firstrRun,
            int tpId,
            int siteId,
            out List<TpCustBalanceModel> balanceDatas)
        {
            balanceDatas = new List<TpCustBalanceModel>();

            _logger.LogInformation($"GetBalanceWorker-GetBalance-Start_"
                                   + $"TpId={tpId} SiteId={siteId} "
                                   + $"StartTime={DateTime.UtcNow.AddHours(8)}");

            var custInfoList = GetTpCustInfoList(firstrRun, tpId, siteId);
            var tpCurrencyId = "";

            if (tpId == (int)TpEnum.Saba)
            {
                if (!GetBalanceAtSameTime(tpId, siteId, custInfoList, tpCurrencyId,
                    out balanceDatas))
                {
                    return false;
                }
            }
            else if (tpId == (int)TpEnum.NineW)
            {
                var currencyList = custInfoList.GroupBy(g => g.TpCurrencyId);

                foreach (var tpCurData in currencyList)
                {
                    tpCurrencyId = tpCurData.Key;
                    if (!GetBalanceAtSameTime(tpId, siteId, tpCurData, tpCurrencyId,
                                        out List<TpCustBalanceModel> tmpBalanceDatas))
                    {
                        return false;
                    }
                    balanceDatas.AddRange(tmpBalanceDatas);
                }
            }
            else
            {
                if (!GetBalanceInOrder(custInfoList,
                    out balanceDatas))
                {
                    return false;
                }
            }

            _logger.LogInformation($"GetBalanceWorker-GetBalance-End_"
                                   + $"TpId={tpId} SiteId={siteId} "
                                   + $"EndTime={DateTime.UtcNow.AddHours(8)}");
            return true;
        }

        public ApiResult SetBetLimit(SetBetLimit setBetLimit)
        {
            var apiResult = new ApiResult();
            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(setBetLimit.CustId, setBetLimit.TpId);
            if (tpCustInfoResult.Data == null)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                apiResult.Message = string.IsNullOrEmpty(tpCustInfoResult.Message)
                    ? tpCustInfoResult.RawMessage
                    : tpCustInfoResult.Message;
                apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
                return apiResult;
            }

            var tpHelper = (IThirdPartyBetLimitParticular)thirdPartyHelper.GetThirdPartyHelper(setBetLimit.TpId,
                tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel =
                thirdPartyHelper.GetThirdPartyBySite(setBetLimit.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            return tpHelper.SetBetLimit(tpCustInfoResult.Data, inputs, setBetLimit.BetSetting);
        }

        /// <summary>
        /// 新增餘額資料
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <param name="timeString">資料所屬時間</param>
        /// <param name="datas">資料</param>
        public void InsertBalance(string timeString,
            List<TpCustBalanceModel> datas)
        {
            _logger.LogInformation($"GetBalanceWorker-InsertBalance-Start_"
                                   + $"StartTime={DateTime.UtcNow.AddHours(8)}");

            foreach (var data in datas.OrderBy(x => x.TpId).ThenBy(x => x.CustId).ThenBy(x => x.RequestTime))
            {
                var result = InsertBalance(timeString, data);

                if (result == 0)
                {
                    _logger.LogWarning($"GetBalanceWorker-InsertBalance-Warning_"
                                       + $"TimeString={timeString}"
                                       + $"BalanceData={JsonConvert.SerializeObject(data)}"
                                       + "Insert balance data fail.");
                }
            }

            _logger.LogInformation($"GetBalanceWorker-InsertBalance-End_"
                                   + $"EndTime={DateTime.UtcNow.AddHours(8)}");
        }

        /// <summary>
        /// 取得最近一次執行排程時間
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <returns></returns>
        public DateTime? GetLatestRunTime()
        {
            var result = GetLatestRunTimeFromTable();

            if (string.IsNullOrWhiteSpace(result))
            {
                return null;
            }
            else
            {
                return Convert.ToDateTime(result);
            }
        }

        public ApiResult<object> GetBetDetail(GetBetDetailRequest request)
        {
            if (!_tpGameTypeConfig.TryGetValue(request.GameType, out var gameTypeOption))
            {
                return new ApiResult<object>()
                {
                    ErrorCode = ErrorCode.InvalidParameter,
                    Message = "GameType not found."
                };
            }
            gameTypeOption.TpName = GetTpName(gameTypeOption.TpId);

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(request.CustId, gameTypeOption.TpId);
            if (tpCustInfoResult.Data == null)
            {
                return new ApiResult<object>()
                {
                    ErrorCode = tpCustInfoResult.ErrorCode,
                    Message = tpCustInfoResult.RawMessage
                };
            }

            var tpHelper = (IThirdPartyTpBetRecordDetail)thirdPartyHelper.GetThirdPartyHelper(gameTypeOption.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyBySite(gameTypeOption.TpId, tpCustInfoResult.Data.SiteId).Setting);
            var getBetDetail = new GetBetDetail(request, gameTypeOption);
            var result = tpHelper.GetBetDetail(getBetDetail, tpCustInfoResult.Data, inputs);
            return new ApiResult<object>(ErrorCode.Success, "success", result);
        }

        /// <summary>
        /// 取的商戶餘額
        /// </summary>
        /// <param name="tpMerchantInfo"></param>
        /// <returns></returns>
        public ApiResult<MerchantBalanceResponse> GetMerchantBalance(TpMerchantInfo tpMerchantInfo)
        {
            var tpHelper = (IThirdPartyTpGetMerchantBalance)thirdPartyHelper.GetThirdPartyHelper(tpMerchantInfo.TpId, tpMerchantInfo.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpMerchantInfo.TpId, tpMerchantInfo.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
            var merchantBalanceResponse = tpHelper.GetMerchantBalance(tpMerchantInfo, inputs);
            return merchantBalanceResponse;
        }

        public ApiResult<TpBetRecordExtend> GetTpBetRecordExtend(GetTpBetRecordExtendRequest request)
        {
            var tpId = request.BetRecordInfo.TpId;

            if (!_tpGameTypeConfig.TryGetValue(request.BetRecordInfo.GameType, out var gameTypeOption))
            {
                return new ApiResult<TpBetRecordExtend>()
                {
                    ErrorCode = ErrorCode.InvalidParameter,
                    RawMessage = "GameType not found."
                };
            }
            gameTypeOption.TpName = GetTpName(tpId);

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(request.CustId, tpId);
            if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            {
                return new ApiResult<TpBetRecordExtend>()
                {
                    ErrorCode = tpCustInfoResult.ErrorCode,
                    RawMessage = tpCustInfoResult.RawMessage
                };
            }
            tpCustInfoResult.Data.IsoCode = GetCurrencyIsoCode(request.CurrencyId);

            var tpHelper = (IThirdPartyTpBetRecordDetail)thirdPartyHelper.GetThirdPartyHelper(tpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyBySite(tpId, tpCustInfoResult.Data.SiteId).Setting);

            try
            {
                var result = tpHelper.GetTpBetRecordExtend(request.BetRecordInfo, gameTypeOption, tpCustInfoResult.Data, inputs);
                return new ApiResult<TpBetRecordExtend>(result);
            }
            catch (NotImplementedException)
            {
                return new ApiResult<TpBetRecordExtend>(new TpBetRecordExtend(request.BetRecordInfo, gameTypeOption, tpCustInfoResult.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ex.Message} ||  {JsonConvert.SerializeObject(request.BetRecordInfo)}");
                return new ApiResult<TpBetRecordExtend>(new TpBetRecordExtend(request.BetRecordInfo, gameTypeOption, tpCustInfoResult.Data));
            }
        }

        public string GetTpName(int tpId)
        {
            return _TpList.TryGetValue(tpId, out var result) ? result.TpName : string.Empty;
        }

        public ApiResult<TpCustInfo> CreateMember(int custId, int tpId, bool isNeedSendRequest)
        {
            return tpCustInfoService.CreateMember(custId, tpId, isNeedSendRequest);
        }

        public ApiResult AddPrizeToPlayer(AddFreeSpin addFreeSpin)
        {
            var apiResult = new ApiResult();

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(addFreeSpin.CustId, addFreeSpin.TpId);
            if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
            {
                var tpCustInfo = tpCustInfoResult.Data;
                var tpHelper = (IThirdPartyEventParticular)thirdPartyHelper.GetThirdPartyHelper(addFreeSpin.TpId, tpCustInfo.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper
                    .GetThirdPartyBySite(addFreeSpin.TpId, tpCustInfo.SiteId).Setting);
                if (addFreeSpin.inputs != null)
                {
                    foreach (var infoI in addFreeSpin.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }

                apiResult = tpHelper.AddPrizeToPlayer(tpCustInfoResult.Data, inputs);
            }
            else
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.Message = tpCustInfoResult.Message;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
            }

            return apiResult;
        }

        public ApiResult CreateBonusCode(CreateBonusCode createBonusCode)
        {
            var apiResult = new ApiResult();

            var tpHelper = (IThirdPartyEventParticular)thirdPartyHelper.GetThirdPartyHelper(createBonusCode.TpId, createBonusCode.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper
                .GetThirdPartyBySite(createBonusCode.TpId, createBonusCode.SiteId).Setting);
            if (createBonusCode.inputs != null)
            {
                foreach (var infoI in createBonusCode.inputs)
                {
                    inputs.Add(infoI.Key, infoI.Value);
                }
            }

            apiResult = tpHelper.CreateBonusCode(createBonusCode.SiteId, inputs);

            return apiResult;
        }

        #region Private Methods

        /// <summary>
        /// 取得最近一次執行排程時間
        /// </summary>
        /// <param name="tpId">資料表日期</param>
        /// <returns></returns>
        private string GetLatestRunTimeFromTable()
        {
            return _tpCustBalanceService.GetLatestRunTime();
        }

        /// <summary>
        /// 取得第三方客戶的清單
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <returns></returns>
        private IEnumerable<BalanceTpCustInfoModel> GetTpCustInfoList(bool firstRun, int tpId, int siteId)
        {
            return tpCustInfoService.GetTpCustInfoList(firstRun, tpId, siteId);
        }

        /// <summary>
        /// 同時取得第三方客戶餘額
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <param name="siteId">平台ID</param>
        /// <param name="custInfoList">客戶資料清單</param>
        /// <param name="tpCurrencyId">第三方幣別</param>
        /// <param name="balanceDatas">餘額資料</param>
        /// <returns></returns>
        private bool GetBalanceAtSameTime(int tpId,
            int siteId,
            IEnumerable<BalanceTpCustInfoModel> custInfoList,
            string tpCurrencyId,
            out List<TpCustBalanceModel> balanceDatas)
        {
            balanceDatas = new List<TpCustBalanceModel>();
            DateTime requestTime = DateTime.UtcNow;
            DateTime responseTime = DateTime.UtcNow;

            try
            {
                var tpHelper = (IThirdPartyGetBalanceParticular)thirdPartyHelper.GetThirdPartyHelper(tpId, siteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpId, siteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
#if DEBUG
                if (!_commonService.IsProdEnvironment() && tpId == (int)TpEnum.NineW)
                {
                    custInfoList = custInfoList.Where(x => !NineWWrongCustList.Any(y => y == x.CustId));
                }
#endif
                var tpUserNameString = string.Join(",", custInfoList.Select(x => x.TpUserName));

                requestTime = DateTime.UtcNow;

                var balanceResult = tpHelper.GetBalance(inputs, tpUserNameString, tpCurrencyId);

                responseTime = DateTime.UtcNow;

                if (balanceResult.ErrorCode != ErrorCode.Success)
                {
                    _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                       + $"TpId={tpId} SiteId={siteId}" +
                                       " Not Success when get this condiction datas.");
                }

                foreach (var cust in custInfoList)
                {
                    var data = balanceResult.Data.Where(x => x.TpUserName == cust.TpUserName)
                        .FirstOrDefault();

                    if (data == null)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                           + $"TpId={tpId} SiteId={siteId}" +
                                           $" CustId={cust.CustId}.Can't get this cust data.");
                        continue;
                    }
                    else if (data.ErrorCode != 0)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                           + $" TpId={tpId} SiteId={siteId}" +
                                           $" CustId={cust.CustId}.Not Success when get this cust data.");
                        continue;
                    }

                    balanceDatas.Add(new TpCustBalanceModel
                    {
                        TpId = cust.TpId,
                        CustId = cust.CustId,
                        SiteId = cust.SiteId,
                        CurrencyId = cust.CurrencyId,
                        Balance = (cust.ExchangeRate > 0 && cust.ExchangeRate != 1)
                            ? data.Balance * cust.ExchangeRate
                            : data.Balance,
                        RequestTime = requestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ResponseTIme = responseTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                return false;
            }
        }

        /// <summary>
        /// 依序取得第三方客戶餘額
        /// </summary>
        /// <param name="custInfoList">客戶資料清單</param>
        /// <param name="balanceDatas">餘額資料</param>
        /// <returns></returns>
        private bool GetBalanceInOrder(IEnumerable<BalanceTpCustInfoModel> custInfoList,
            out List<TpCustBalanceModel> balanceDatas)
        {
            balanceDatas = new List<TpCustBalanceModel>();
            DateTime requestTime = DateTime.UtcNow;
            DateTime responseTime = DateTime.UtcNow;

            try
            {
                foreach (var cust in custInfoList)
                {
#if DEBUG
                    if (!_commonService.IsProdEnvironment() && cust.TpId == (int)TpEnum.PT)
                    {
                        if (PTWrongCustList.Contains(cust.CustId))
                        {
                            continue;
                        }
                    }
#endif
                    requestTime = DateTime.UtcNow;

                    var balanceResult = GetBalance(new TpInfo
                    {
                        TpId = cust.TpId,
                        CustId = cust.CustId,
                        SiteId = cust.SiteId
                    });

                    responseTime = DateTime.UtcNow;

                    if (balanceResult == null)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                           + $" TpId={cust.TpId} SiteId={cust.SiteId}" +
                                           $" CustId={cust.CustId}.Get Data Fail.");
                        continue;
                    }
                    else if (balanceResult.ErrorCode != ErrorCode.Success)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                           + $" TpId={cust.TpId} SiteId={cust.SiteId}" +
                                           $" CustId={cust.CustId}.Not success when get this cust data.");
                        continue;
                    }

                    balanceDatas.Add(new TpCustBalanceModel
                    {
                        TpId = cust.TpId,
                        CustId = cust.CustId,
                        SiteId = cust.SiteId,
                        CurrencyId = cust.CurrencyId,
                        Balance = balanceResult.Data.balance,
                        RequestTime = requestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ResponseTIme = responseTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());
                return false;
            }
        }

        /// <summary>
        /// 新增客戶餘額資料
        /// </summary>
        /// <param name="timeString">所屬其實間</param>
        /// <param name="balanceData">客戶餘額資料</param>
        /// <returns></returns>
        private int InsertBalance(string timeString, TpCustBalanceModel balanceData)
        {
            return _tpCustBalanceService.InsertBalance(timeString, balanceData);
        }

        private bool ThirdPartyIsMaintain(int siteId, int tpId)
        {
            var maintainList = GetTpMaintainListWithCache(siteId);
            return maintainList.Any(x => x.TpId == tpId
                && x.TpGameType == 0); // TpGameType: 0 第三方整個進維護
        }

        private IEnumerable<TpMaintain> GetTpMaintainListWithCache(int siteId)
        {
            var key = $"GetTpMaintainList{siteId}";

            return _cacheHelper.TryGetOrCreate(key,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)),
                i => thirdPartyHelper.GetTpMaintainList(siteId)
                    .Where(item => item.MaintainCheck(DateTime.UtcNow)));
        }

        private decimal DealTransferAmount(decimal amount, decimal exchangeRate, int tpId, out decimal credit, out bool execute, out bool skip)
        {
            execute = true;
            skip = true;
            amount = Math.Truncate(amount);

            if (exchangeRate != 0)
            {
                amount /= exchangeRate;
            }
            credit = amount;

            // only check mega
            if (tpId != (int)TpEnum.Mega)
            {
                return amount;
            }

            skip = false;
            // check 2 decimal place
            var checkAmount = amount * 100;
            bool hasMoreThan3decimals = checkAmount != (int)(checkAmount);
            // beyond 2 decimal
            if (hasMoreThan3decimals)
            {
                // only transfer int amount
                amount = (int)amount;
                // no execute if int amount is 0
                if (Math.Abs(amount) == 0)
                {
                    execute = false;
                }
            }

            return amount;
        }

        private string GetCurrencyIsoCode(int currencyId)
        {
            return _currencyList.GetValueOrDefault(currencyId).IsoCode;
        }

        private void SetRecentlyPlayed(TpInfo tpInfo)
        {
            try
            {
                if (!int.TryParse(tpInfo.inputs.GetValueOrDefault("tpGameType"), out var tpGameType))
                {
                    _logger.LogError($"SetRecentlyPlayed tpGameType Error. tpInfo:{JsonConvert.SerializeObject(tpInfo)}");
                    return;
                }

                if (!_tpGameTypeConfig.TryGetValue(tpGameType, out var gameTypeOption))
                {
                    _logger.LogError($"SetRecentlyPlayed _tpGameTypeConfig Error. tpInfo:{JsonConvert.SerializeObject(tpInfo)}");
                    return;
                }

                if (gameTypeOption.CategoryId == TpCategory.Slots)
                {
                    _mainDbClient.Execute("Member_RecentlyPlayed_Set", new
                    {
                        tpInfo.CustId,
                        GameInfo = $"{tpInfo.TpId}_{tpInfo.inputs.GetValueOrDefault("gameId")}"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        private void SetRecentlyViewed(TpInfo tpInfo)
        {
            try
            {
                if (!int.TryParse(tpInfo.inputs.GetValueOrDefault("tpGameType"), out var tpGameType))
                {
                    _logger.LogWarning($"SetRecentlyViewed tpGameType Error. tpInfo:{JsonConvert.SerializeObject(tpInfo)}");
                    return;
                }

                if (!_tpGameTypeConfig.TryGetValue(tpGameType, out var gameTypeOption))
                {
                    _logger.LogWarning($"SetRecentlyViewed _tpGameTypeConfig Error. tpInfo:{JsonConvert.SerializeObject(tpInfo)}");
                    return;
                }
                else
                {
                    var gameId = tpInfo.inputs.ContainsKey("gameId") ? tpInfo.inputs["gameId"] : "0";
                    gameId = (gameId == "undefined" || gameId == "null") ? "" : gameId;
                    var betType = tpInfo.inputs.ContainsKey("bettype") ? tpInfo.inputs["bettype"] : "0";

                    // 顯示遊戲大廳名 (Dream Gaming)
                    var type = (int)RecentlyViewedTypeEnum.LobbyName;
                    // 類型_TP_TpGameType_CategoryId_GameId_BetType
                    var gameInfo = $"{type}:{tpInfo.TpId}:{tpGameType}:{(int)gameTypeOption.CategoryId}:{gameId}:0";

                    // Slot 顯示遊戲名稱
                    if (gameTypeOption.CategoryId == TpCategory.Slots)
                    {
                        if (tpInfo.TpId != (int)TpEnum.SBSW)
                        {
                            // 顯示遊戲名稱 (Golden Grimoire)
                            type = (int)RecentlyViewedTypeEnum.GameName;
                            gameInfo = $"{type}:{tpInfo.TpId}:{tpGameType}:{(int)gameTypeOption.CategoryId}:{gameId}:0";
                        }
                    }
                    else if (tpInfo.TpId == (int)TpEnum.SBSW)
                    {
                        // esport games 多塞bettype判斷
                        type = (int)RecentlyViewedTypeEnum.LobbyName;
                        gameInfo = $"{type}:{tpInfo.TpId}:{tpGameType}:{(int)gameTypeOption.CategoryId}:{gameId}:{betType}";
                    }

                    _mainDbClient.Execute("Member_CustRecentlyViewed_Set", new
                    {
                        _CustId = tpInfo.CustId,
                        _RecentlyViewed = gameInfo
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
        }

        #endregion Private Methods
    }
}