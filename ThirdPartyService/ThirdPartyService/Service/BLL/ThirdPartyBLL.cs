using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpTransferInfo tpTransferInfo;
        private readonly TpTransferBonusInfoService tpTransferBonusInfoService;

        public ThirdPartyBLL(IConfiguration configuration, IWebHostEnvironment env, TpCustInfoService tpCustInfoService, ThirdPartyHelper thirdPartyHelper, TpTransferInfo tpTransferInfo, TpTransferBonusInfoService tpTransferBonusInfoService, AuthBLL authBLL)
        {
            this.tpCustInfoService = tpCustInfoService;
            this.thirdPartyHelper = thirdPartyHelper;
            this.tpTransferInfo = tpTransferInfo;
            this.env = env;
            this.configuration = configuration;
            ThirdPartyHelperBase.AuthProvider = authBLL;
            this.tpTransferBonusInfoService = tpTransferBonusInfoService;
        }
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
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage;/*3RD Error Message*/
                return apiResult;
            }

            IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyBySite(tpTransfer.TpId, tpCustInfoResult.Data.SiteId);
            var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);

            var balanceApiResult = tpHelper.GetBalance(tpCustInfoResult.Data, inputs);
            if (balanceApiResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = ErrorCode.ServerIsBusy;
                apiResult.RawErrorCode = balanceApiResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = balanceApiResult.RawMessage;/*3RD Error Message*/
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
            var tpTransferReqResult = tpTransferInfo.TpTransferReq(tpTransfer, tpCustInfoResult.Data, thirdId, out transId);
            if (tpTransferReqResult.ErrorCode != ErrorCode.Success)
            {
                return tpTransferReqResult;
            }

            decimal amount = tpTransfer.Amount;

            if (tpCustInfoResult.Data.ExchangeRate > 0 && tpCustInfoResult.Data.ExchangeRate != 1)
            {
                amount = tpTransfer.Amount / tpCustInfoResult.Data.ExchangeRate;
            }

            apiResult = tpHelper.Transfer(tpCustInfoResult.Data, inputs, amount, tpTransfer.direction, transId, thirdId);

            tpTransferInfo.TpTransferCommit(apiResult, tpCustInfoResult.Data, tpTransfer, transId);
            /*Use Async to Record Log*/
            if (apiResult.ErrorCode == ErrorCode.Success)
            {
                Task.Run(() =>
                {
                    tpCustInfoService.SetTpLastTradingTime(tpTransfer.CustId, tpTransfer.TpId);
                });
            }
            return apiResult;
        }

        public ApiResult<UserBalance> GetBalance(TpInfo tpInfo)
        {
            var apiResult = new ApiResult<UserBalance>();
            apiResult.ErrorCode = ErrorCode.SystemError;
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

            var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpInfo.CustId, tpInfo.TpId);
            if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
            {
                // get auto transfer status
                var custWallet = tpCustInfoService.GetCustWallet(tpInfo.CustId);
                if (custWallet == null)
                {
                    apiResult.ErrorCode = ErrorCode.UserNotExist;
                    apiResult.Message = "This Account Cust Wallet Not Found";
                    return apiResult;
                }

                // check auto transfer condition
                if (custWallet.AutoTransfer == (int)AutoType.Auto && tpInfo.TpId != custWallet.InGame)
                {
                    var tpCustInfo = tpCustInfoResult.Data;
                    var tpBySite = thirdPartyHelper.GetThirdPartyBySite(tpInfo.TpId, tpCustInfo.SiteId);
                    var walletType = tpBySite.WalletType;
                    int fromTpId = custWallet.InGame;
                    int toTpId = tpInfo.TpId;

                    TpTransfer transfer = new TpTransfer();
                    Dictionary<string, string> input = new Dictionary<string, string>();

                    if (walletType == (int)WalletType.Transfer)
                    {
                        if (custWallet.Balance <= 0)
                        {
                            // transfer game to transfer game need trasfer to main wallet first
                            if (custWallet.InGame > 0 && tpInfo.TpId > 0)
                            {
                                // get third-party balance
                                var preTpInfo = tpInfo;
                                preTpInfo.TpId = custWallet.InGame;
                                var preBalance = GetBalance(preTpInfo);
                                tpInfo.TpId = toTpId;
                                if (preBalance.Data.balance <= 0)
                                {
                                    apiResult.ErrorCode = ErrorCode.Success;
                                    apiResult.Message = "Balance In Is No Need To Transfer(3)";
                                    apiResult.Data.ToTpId = tpInfo.TpId;
                                    return apiResult;
                                }

                                Dictionary<string, string> preInput = new Dictionary<string, string>();
                                transfer.TpId = custWallet.InGame;
                                transfer.Amount = 1; // temporary use 1 for pass validation
                                transfer.direction = Direction.ToMain;
                                transfer.CustId = tpInfo.CustId;
                                preInput.Add("AllOut", "Y");
                                transfer.inputs = preInput;
                                int preTpId = 0;

                                var preTransfer = Transfer(transfer);
                                if (preTransfer.ErrorCode == ErrorCode.Success)
                                {
                                    tpCustInfoService.SetCustWallet(tpInfo.CustId, preTpId);
                                    var preCustWallet = tpCustInfoService.GetCustWallet(tpInfo.CustId);
                                    custWallet.Balance = preCustWallet.Balance;
                                }
                            }
                            else
                            {
                                apiResult.ErrorCode = ErrorCode.Success;
                                apiResult.Message = "Balance In Is No Need To Transfer";
                                apiResult.Data.ToTpId = tpInfo.TpId;
                                return apiResult;
                            }
                        }

                        transfer.TpId = tpInfo.TpId;
                        transfer.Amount = custWallet.Balance;
                        transfer.direction = Direction.ToTp;
                        transfer.CustId = tpInfo.CustId;
                        input.Add("AllIn", "Y");
                        transfer.inputs = input;
                        toTpId = tpInfo.TpId;
                    }
                    else
                    {
                        if (custWallet.InGame == 0)
                        {
                            apiResult.ErrorCode = ErrorCode.OTHER_ERROR;
                            apiResult.Message = "Balance Out Is No Need To Transfer";
                            apiResult.Data.ToTpId = custWallet.InGame;
                            return apiResult;
                        }

                        transfer.TpId = custWallet.InGame;
                        transfer.Amount = 1; // temporary use 1 for pass validation
                        transfer.direction = Direction.ToMain;
                        transfer.CustId = tpInfo.CustId;
                        input.Add("AllOut", "Y");
                        transfer.inputs = input;
                        toTpId = 0;
                    }

                    var doTransfer = Transfer(transfer);
                    apiResult.ErrorCode = doTransfer.ErrorCode;
                    apiResult.Message = (doTransfer.ErrorCode == ErrorCode.Success) ? $"From {fromTpId} To {toTpId}" : doTransfer.Message;
                    apiResult.RawErrorCode = doTransfer.RawErrorCode;
                    apiResult.RawMessage = doTransfer.RawMessage;
                    apiResult.Data.ToTpId = toTpId;
                }
                else
                {
                    apiResult.ErrorCode = ErrorCode.OTHER_ERROR;
                    apiResult.Message = "This Account Not Allow Auto Transfer";
                }
            }
            else
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.Message = tpCustInfoResult.Message;
            }

            return apiResult;
        }
    }
}
