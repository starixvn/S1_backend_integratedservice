using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;
using ThirdPartyService.Extensions;
using System.Transactions;

namespace ThirdPartyService.Service.BLL
{
    public class ThirdPartyBLL
    {
        #region Fields

        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;
        private readonly ILogger<ThirdPartyBLL> _logger;
        private readonly Log2File log2File;

        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpTransferInfo tpTransferInfo;

        private readonly TpCustInfoService tpCustInfoService;
        private readonly TpTransferBonusInfoService tpTransferBonusInfoService;
        private readonly ServerInfoService _serverInfoService;
        private readonly TpCustBalanceService _tpCustBalanceService;
#if DEBUG
        /// <summary>
        /// PT異常客戶清單(測試機)，共13筆
        /// </summary>
        private readonly int[] PTWrongCustList = new int[] { 1342, 249646, 249688, 249768, 249782, 249784, 249827, 249832, 249835, 249862, 249863, 249864, 249892 };

        /// <summary>
        /// 9W異常客戶清單(測試機)，共12筆
        /// </summary>
        private readonly int[] NineWWrongCustList = new int[] { 565, 602, 1017, 127559, 249667, 249745, 249814, 249874, 249951, 250066, 249951, 250057 };
#endif
        #endregion

        #region Constructors

        public ThirdPartyBLL(IConfiguration configuration,
                                 IWebHostEnvironment env,
                                 ILogger<ThirdPartyBLL> logger,
                                 Log2File log2File,
                                 ThirdPartyHelper thirdPartyHelper,
                                 TpTransferInfo tpTransferInfo,
                                 TpCustInfoService tpCustInfoService,
                                 TpTransferBonusInfoService tpTransferBonusInfoService,
                                 ServerInfoService serverInfoService,
                                 TpCustBalanceService tpCustBalanceService,
                                 AuthBLL authBLL)
        {
            this.env = env;
            this.configuration = configuration;
            _logger = logger;
            this.log2File = log2File;

            this.thirdPartyHelper = thirdPartyHelper;
            this.tpTransferInfo = tpTransferInfo;

            this.tpCustInfoService = tpCustInfoService;
            this.tpTransferBonusInfoService = tpTransferBonusInfoService;
            _serverInfoService = serverInfoService;
            _tpCustBalanceService = tpCustBalanceService;

            ThirdPartyHelperBase.AuthProvider = authBLL;
            ThirdPartyHelperBase.ContentRootPath = env.ContentRootPath;
        }

        #endregion

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
            if (tpInfo.inputs.ContainsKey("fun") && tpInfo.inputs["fun"].Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpInfo.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpInfo.SiteId).Setting);
                if (tpInfo.inputs != null)
                {
                    foreach (var infoI in tpInfo.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }

                TpCustInfo freeCustInfo = new TpCustInfo();
                apiResult = tpHelper.Login(freeCustInfo, (AppType)tpInfo.AppType, tpLangId, inputs);
                return apiResult;
            }

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
            if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
            {
                var tpCustInfo = tpCustInfoResult.Data;
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfo.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfo.SiteId).Setting);
                if (tpInfo.inputs != null)
                {
                    foreach (var infoI in tpInfo.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }

                var configEnv = configuration.GetValue<string>("Environment");

                // Test account only allow login for saba
                if (tpCustInfoResult.Data.IsTest && env.EnvironmentName == "Production" && configEnv == null && tpInfo.TpId != 1)
                {
                    apiResult.ErrorCode = ErrorCode.NoAuthority;
                    apiResult.RawMessage = "This Account not allow for access";
                }
                else
                {
                    apiResult = tpHelper.Login(tpCustInfoResult.Data, (AppType)tpInfo.AppType, tpLangId, inputs);
                }
                /*Use Async to Record Log*/
                Task.Run(() =>
                {
                    tpCustInfoService.SetTpLastLoginTime(tpInfo.CustId, tpInfo.TpId);
                });
            }
            else
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.Message = tpCustInfoResult.Message;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage;/*3RD Error Message*/
            }

            return apiResult;
        }

        public ApiResult<TransferResult> Transfer(TpTransfer tpTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();
            try
            {
                string prefix = "T";

                if (tpTransfer.direction != Direction.ToMain && tpTransfer.direction != Direction.ToTp)
                {
                    apiResult.ErrorCode = ErrorCode.InvalidParameter;
                    return apiResult;
                }
                else if (tpTransfer.Amount <= 0)
                {
                    apiResult.ErrorCode = ErrorCode.InvalidParameter;
                    return apiResult;
                }

                // source autotransfer running bonus failed
                if (tpTransfer.inputs != null && tpTransfer.inputs.ContainsKey("Source") &&
                    tpTransfer.inputs["Source"] == "Auto")
                {
                    prefix = "AT";
                    var custBonus = thirdPartyHelper.CheckBonusWallet(tpTransfer.CustId, tpTransfer.TpId);
                    if (custBonus != null)
                    {
                        apiResult.ErrorCode = ErrorCode.AutoTransferBonusFail;
                        return apiResult;
                    }
                }

                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpTransfer.CustId, tpTransfer.TpId);

                // Test account create 3rd party only for saba
                if (tpCustInfoResult.Data.IsTest && env.EnvironmentName == "Production" && tpTransfer.TpId != 1)
                {
                    apiResult.ErrorCode = ErrorCode.NoAuthority;
                    apiResult.RawMessage = "This Account is for Testing.";
                    return apiResult;
                }
                else if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
                {
                    apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                    apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode; /*3RD Error Code*/
                    apiResult.RawMessage = tpCustInfoResult.RawMessage; /*3RD Error Message*/
                    return apiResult;
                }

                IThirdPartyHelper tpHelper =
                    thirdPartyHelper.GetThirdPartyHelper(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel =
                    thirdPartyHelper.GetThirdPartyBySite(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
                if (balanceApiResult.ErrorCode != ErrorCode.Success)
                {
                    apiResult.ErrorCode = ErrorCode.ServerIsBusy;
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

                string transId = null;
                var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
                string thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;
                var tpTransferReqResult =
                    tpTransferInfo.TpTransferReqV2(tpTransfer, tpCustInfoResult.Data, thirdId, prefix, out transId);
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

        public ApiResult<UserBalance> GetBalance(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.SystemError
            };

            try
            {
                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
                if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
                {
                    IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                    var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                    var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

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
                var tpHelper = (IThirdPartyParticular)thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
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
            if (tpTransferInfoModel.Direction == (int)Direction.ToTp && tpTransferInfoModel.Status == (int)TransferStatus.Pending)
            {
                bool isSuccess = tpTransferInfo.SetTransferFail(tpTransferInfoModel.TransId);
            }
            else
            {
                //tpTransferInfoModel.CustId
                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpTransferInfoModel.CustId, tpTransferInfoModel.TpId);
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpTransferInfoModel.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

                decimal amount = tpTransferInfoModel.Amount;
                if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
                {
                    amount = tpTransferInfoModel.Amount / tpCustInfoResult.Data.ExchangeRate;
                }

                var apiResult = tpHelper.CheckTransfer(tpCustInfoResult.Data, inputs, amount, (Direction)tpTransferInfoModel.Direction, tpTransferInfoModel.TransId, tpTransferInfoModel.ThirdId);
                TpTransfer tpTransfer = new TpTransfer();
                tpTransfer.direction = (Direction)tpTransferInfoModel.Direction;
                tpTransfer.Amount = tpTransferInfoModel.Amount;
                tpTransfer.TpId = tpTransferInfoModel.TpId;

                tpTransferInfo.TpTransferCommit(apiResult, tpCustInfoResult.Data, tpTransfer, tpTransferInfoModel.TransId);
            }
        }
        public ApiResult BonusTransfer(TpBonusTransfer tpBonusTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpBonusTransfer.CustId, tpBonusTransfer.TpId);

            // Test account create 3rd party only for saba
            if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage;/*3RD Error Message*/
                return apiResult;
            }

            IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpBonusTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var tpHelperParticular = (IThirdPartyParticular)thirdPartyHelper.GetThirdPartyHelper(tpBonusTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpBonusTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = balanceApiResult.RawMessage;/*3RD Error Message*/
                return apiResult;
            }

            string transId = null;
            var thirdIdResult = tpHelper.GetThirdId(tpCustInfoResult.Data, inputs);
            string thirdId = thirdIdResult.Implemented ? thirdIdResult.ThirdId : null;
            var tpTransferReqResult = tpTransferBonusInfoService.TpTransferBonusReq(tpBonusTransfer, tpCustInfoResult.Data, thirdId, out transId, out int bonusId);
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

            apiResult = apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, bonus, direction: Direction.ToTp, transId, thirdId); /*tpHelperParticular.BonusTransfer(tpCustInfoResult.Data, inputs, bonusAmount, transId, thirdId);*/

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
        public ApiResult FixNegative(TpInfo tpInfo)
        {
            var apiResult = new ApiResult();

            apiResult.ErrorCode = ErrorCode.SystemError;
            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
            if (tpCustInfoResult.Data != null)
            {
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfoResult.Data.SiteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
                var balanceResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
                if (balanceResult.Data.balance < 0)
                {
                    apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, Math.Abs(balanceResult.Data.balance), Direction.ToTp, Guid.NewGuid().ToString("N"), null);
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.TransferFail;
                    apiResult.Message = $"{tpCustInfoResult.Data.TpUserName}'s balance is {balanceResult.Data.balance.ToString("#,##0.##")}, It's not Negative.";
                }
            }

            return apiResult;
        }

        public ApiResult<TransferResult> AdjustWallet(AdminTpTransfer adminTpTransfer)
        {
            var apiResult = new ApiResult<TransferResult>();

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
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage;/*3RD Error Message*/
                return apiResult;
            }

            IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(adminTpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(adminTpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = balanceApiResult.RawMessage;/*3RD Error Message*/
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
            apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, amount, adminTpTransfer.direction, transId, thirdId);
            tpTransferInfo.SetAdjustWalletLog(apiResult, transId);

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
                log2File.Error(JsonConvert.SerializeObject(apiResult), "AutoTransfer");
                return apiResult;
            }
            apiResult.Data.MainBalanceBefore = custWallet.Balance.ToString();

            if (custWallet.AutoTransfer != (int)AutoType.Auto)
            {
                apiResult.ErrorCode = ErrorCode.AutoTransferIsOff;
                apiResult.Message = "This Account Auto Transfer Is Off";
                log2File.Error(JsonConvert.SerializeObject(apiResult), "AutoTransfer");
                return apiResult;
            }
            apiResult.Data.AutoTransfer = custWallet.AutoTransfer;
            log2File.Info($"Main Before : \r\n{JsonConvert.SerializeObject(apiResult)}", "AutoTransfer");

            TpTransfer transfer = new TpTransfer();
            var subResult = new ApiResult<AutoTransferResult>();
            subResult = apiResult;

            // get all transfer games and status is on
            var transferGames = thirdPartyHelper.GetThirdPartyByWalletType((int)WalletType.Transfer, custWallet.SiteId);
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
                        toTransferGame = true;
                        //continue;
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
                                log2File.Info($"Game To Main : \r\n{JsonConvert.SerializeObject(subResult)}",
                                    "AutoTransfer");
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
                    apiResult.Data.MainBalanceAfter = (doTransfer.ErrorCode == ErrorCode.Success) ? custWallet.Balance.ToString() : "--";
                    apiResult.Data.GameBalance = "--";
                    apiResult.ErrorCode = doTransfer.ErrorCode;
                    apiResult.Message = doTransfer.Message;
                    apiResult.RawErrorCode = doTransfer.RawErrorCode;
                    apiResult.RawMessage = doTransfer.RawMessage;
                    log2File.Info($"Main After : \r\n{JsonConvert.SerializeObject(apiResult)}", "AutoTransfer");
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.AutoTransferIsSeamless;
                    apiResult.Message = "Go To Seamless Game Is No Need To Transfer";
                    log2File.Error(JsonConvert.SerializeObject(apiResult), "AutoTransfer");
                }
            }
            else
            {
                apiResult.ErrorCode = ErrorCode.AutoTransferFail;
                apiResult.Message = "Get Transfer Games Failed";
                log2File.Error(JsonConvert.SerializeObject(apiResult), "AutoTransfer");
            }
            return apiResult;
        }

        /// <summary>
        /// 取指定條件玩家的第三方餘額
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <param name="siteId">平台ID</param>
        /// <param name="balanceDatas">餘額資料</param>
        public void GetBalance(int tpId,
                               int siteId,
                               out List<TpCustBalanceModel> balanceDatas)
        {
            balanceDatas = new List<TpCustBalanceModel>();

            _logger.LogInformation($"GetBalanceWorker-GetBalance-Start_"
                                   + $"TpId={tpId} SiteId={siteId} "
                                   + $"EndTime={DateTime.UtcNow.AddHours(8)}");

            //log2File.Info($"GetBalanceWorker-GetBalance-Start_"
            //                       + $"TpId={tpId} SiteId={siteId} "
            //                       + $"EndTime={DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

            var custInfoList = GetTpCustInfoList(tpId, siteId);

            if (tpId == (int)TpEnum.NineW || tpId == (int)TpEnum.Saba)
            {
                if (GetBalanceAtSameTime(tpId, siteId, custInfoList,
                                         out List<TpCustBalanceModel> datasResult))
                {
                    balanceDatas.AddRange(datasResult);
                }
            }
            else
            {
                if (GetBalanceInOrder(custInfoList,
                                         out List<TpCustBalanceModel> datasResult))
                {
                    balanceDatas.AddRange(datasResult);
                }
            }

            _logger.LogInformation($"GetBalanceWorker-GetBalance-End_"
                                   + $"TpId={tpId} SiteId={siteId} "
                                   + $"EndTime={DateTime.UtcNow.AddHours(8)}");

            //log2File.Info($"GetBalanceWorker-GetBalance-End_"
            //                       + $"TpId={tpId} SiteId={siteId} "
            //                       + $"EndTime={DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

            return;
        }

        /// <summary>
        /// 新增餘額資料
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <param name="timeString">資料所屬時間</param>
        /// <param name="datas">資料</param>
        public void InsertBalance(string tableDate,
                                  string timeString,
                                  List<TpCustBalanceModel> datas)
        {

            _logger.LogInformation($"GetBalanceWorker-InsertBalance-Start_"
                                       + $"StartTime={DateTime.UtcNow.AddHours(8)}");

            //log2File.Info($"GetBalanceWorker-InsertBalance-Start_"
            //                           + $"StartTime={DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

            foreach (var data in datas.OrderBy(x => x.TpId).ThenBy(x => x.CustId))
            {
                var result = InsertBalance(tableDate, timeString, data);

                if (result == 0)
                {
                    _logger.LogWarning($"GetBalanceWorker-InsertBalance-Warning_"
                                       + $"TableDate={tableDate} TimeString={timeString}"
                                       + $"BalanceData={JsonConvert.SerializeObject(data)}"
                                       + "Insert balance data fail.");

                    //log2File.Error($"GetBalanceWorker-InsertBalance-Warning_"
                    //                   + $"TableDate={tableDate} TimeString={timeString}"
                    //                   + $"BalanceData={JsonConvert.SerializeObject(data)}"
                    //                   + "Insert balance data fail.", "GetBalanceWorker");
                }
            }

            _logger.LogInformation($"GetBalanceWorker-InsertBalance-End_"
                                   + $"EndTime={DateTime.UtcNow.AddHours(8)}");

            //log2File.Info($"GetBalanceWorker-InsertBalance-End_"
            //                       + $"EndTime={DateTime.UtcNow.AddHours(8)}", "GetBalanceWorker");

        }

        /// <summary>
        /// 取得最近一次執行排程時間
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public DateTime? GetLastestRunTime(string tableDate)
        {
            int i = 0; bool tableExist = false;
            for (i = Convert.ToInt32(tableDate); i > 20210701; i--)
            {
                if (CheckTpCustBalanceCourseTable(i.ToString()))
                {
                    tableExist = true;
                    break;
                }
            }

            if (tableExist)
            {
                return GetLastestRunTime(i);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 檢查第三方餘額資料表是否存在
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public bool CheckTpCustBalanceCourseTable(string tableDate)
        {
            var checkResult = _tpCustBalanceService.CheckTpCustBalanceCourseTable(tableDate);

            if (checkResult != $"TpCustBalanceCourse_{tableDate}")
            {
                return false; //not exist;
            }
            else
            {
                return true; //exist;
            }
        }

        /// <summary>
        /// 新增第三方餘額資料表
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        public bool CreateTpCustBalanceCourseTable(string tableDate)
        {
            return _tpCustBalanceService.CreateTpCustBalanceCourseTable(tableDate);
        }

        #region Private Methods

        /// <summary>
        /// 取得最近一次執行排程時間
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <returns></returns>
        private DateTime GetLastestRunTime(int tableDate)
        {
            return Convert.ToDateTime(_tpCustBalanceService.GetLastestRunTime(tableDate.ToString()));
        }

        /// <summary>
        /// 取得第三方客戶的清單
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <returns></returns>
        private IEnumerable<BalanceTpCustInfoModel> GetTpCustInfoList(int tpId, int siteId)
        {
            return tpCustInfoService.GetTpCustInfoList(tpId, siteId);
        }

        /// <summary>
        /// 同時取得第三方客戶餘額
        /// </summary>
        /// <param name="tpId">第三方ID</param>
        /// <param name="siteId">平台ID</param>
        /// <param name="custInfoList">客戶資料清單</param>
        /// <param name="balanceDatas">餘額資料</param>
        /// <returns></returns>
        private bool GetBalanceAtSameTime(int tpId,
                                          int siteId,
                                          IEnumerable<BalanceTpCustInfoModel> custInfoList,
                                          out List<TpCustBalanceModel> balanceDatas)
        {
            balanceDatas = new List<TpCustBalanceModel>();

            try
            {
                var tpHelper = (IThirdPartyGetBalanceParticular)thirdPartyHelper.GetThirdPartyHelper(tpId, siteId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpId, siteId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
#if DEBUG
                if (env.EnvironmentName != "Production" && tpId == (int)TpEnum.NineW)
                {
                    custInfoList = custInfoList.Where(x => !NineWWrongCustList.Any(y => y == x.CustId));
                }
#endif
                var tpUserNameString = string.Join(",", custInfoList.Select(x => x.TpUserName));

                var balanceResult = tpHelper.GetBalance(inputs, tpUserNameString);

                if (balanceResult.ErrorCode != ErrorCode.Success)
                {
                    _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                           + $"TpId={tpId} SiteId={siteId}" +
                                           " Not Success when get this condiction datas.");

                    //log2File.Error($"GetBalanceWorker-GetBalance-Warning_"
                    //                       + $"TpId={tpId} SiteId={siteId}" +
                    //                       " Not Success when get this condiction datas.", "GetBalanceWorker");
                    return false;
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

                        //log2File.Error($"GetBalanceWorker-GetBalance-Warning_"
                        //                       + $"TpId={tpId} SiteId={siteId}" +
                        //                       $" CustId={cust.CustId}.Can't get this cust data.", "GetBalanceWorker");
                        continue;
                    }
                    else if (data.ErrorCode != 0)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                               + $" TpId={tpId} SiteId={siteId}" +
                                               $" CustId={cust.CustId}.Not Success when get this cust data.");

                        //log2File.Error($"GetBalanceWorker-GetBalance-Warning_"
                        //                       + $" TpId={tpId} SiteId={siteId}" +
                        //                       $" CustId={cust.CustId}.Not Success when get this cust data.", "GetBalanceWorker");
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
                                  : data.Balance
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());

                //log2File.Error($"GetBalanceWorker-GetBalance-Exception_"
                //                       + $"TpId={tpId} SiteId={siteId} "
                //                       + $"ex={ex}", "GetBalanceWorker");

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

            try
            {
                foreach (var cust in custInfoList)
                {
#if DEBUG
                    if (env.EnvironmentName != "Production" && cust.TpId == (int)TpEnum.PT)
                    {
                        if (PTWrongCustList.Contains(cust.CustId))
                        {
                            continue;
                        }
                    }
#endif
                    var balanceResult = GetBalance(new TpInfo
                    {
                        TpId = cust.TpId,
                        CustId = cust.CustId,
                        SiteId = cust.SiteId
                    });

                    if (balanceResult == null)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                               + $" TpId={cust.TpId} SiteId={cust.SiteId}" +
                                               $" CustId={cust.CustId}.Get Data Fail.");

                        //log2File.Error($"GetBalanceWorker-GetBalance-Warning_"
                        //                       + $" TpId={tpId} SiteId={siteId}" +
                        //                       $" CustId={cust.CustId}.Get Data Fail.", "GetBalanceWorker");
                        continue;
                    }
                    else if (balanceResult.ErrorCode != ErrorCode.Success)
                    {
                        _logger.LogWarning($"GetBalanceWorker-GetBalance-Warning_"
                                               + $" TpId={cust.TpId} SiteId={cust.SiteId}" +
                                               $" CustId={cust.CustId}.Not success when get this cust data.");

                        //log2File.Error($"GetBalanceWorker-GetBalance-Warning_"
                        //                       + $" TpId={tpId} SiteId={siteId}" +
                        //                       $" CustId={cust.CustId}.Not success when get this cust data.", "GetBalanceWorker");
                        continue;
                    }

                    balanceDatas.Add(new TpCustBalanceModel
                    {
                        TpId = cust.TpId,
                        CustId = cust.CustId,
                        SiteId = cust.SiteId,
                        CurrencyId = cust.CurrencyId,
                        Balance = balanceResult.Data.balance
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, MethodExtension.GetCurrentMethod());

                //log2File.Error($"GetBalanceWorker-GetBalance-Exception_"
                //                       + $"TpId={tpId} SiteId={siteId} "
                //                       + $"ex={ex}", "GetBalanceWorker");

                return false;
            }
        }

        /// <summary>
        /// 新增客戶餘額資料
        /// </summary>
        /// <param name="tableDate">資料表日期</param>
        /// <param name="timeString">所屬其實間</param>
        /// <param name="balanceData">客戶餘額資料</param>
        /// <returns></returns>
        private int InsertBalance(string tableDate, string timeString, TpCustBalanceModel balanceData)
        {
            return _tpCustBalanceService.InsertBalance(tableDate, timeString, balanceData);
        }

        #endregion
    }
}
