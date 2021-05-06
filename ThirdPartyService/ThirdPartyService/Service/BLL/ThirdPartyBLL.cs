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
using ThirdPartyService.Service.Library;

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
        private readonly Log2File log2File;

        public ThirdPartyBLL(IConfiguration configuration, IWebHostEnvironment env, TpCustInfoService tpCustInfoService, ThirdPartyHelper thirdPartyHelper, TpTransferInfo tpTransferInfo, TpTransferBonusInfoService tpTransferBonusInfoService, AuthBLL authBLL, Log2File log2File)
        {
            this.tpCustInfoService = tpCustInfoService;
            this.thirdPartyHelper = thirdPartyHelper;
            this.tpTransferInfo = tpTransferInfo;
            this.env = env;
            this.configuration = configuration;
            ThirdPartyHelperBase.AuthProvider = authBLL;
            this.tpTransferBonusInfoService = tpTransferBonusInfoService;
            this.log2File = log2File;
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

            // source autotransfer running bonus failed
            if (tpTransfer.inputs != null && tpTransfer.inputs.ContainsKey("Source") && tpTransfer.inputs["Source"] == "Auto")
            {
                var custBonus = thirdPartyHelper.CheckBonusWallet(tpTransfer.CustId, tpTransfer.TpId);
                if(custBonus != null)
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
                            subResult.Data.GameBalance = (subTrans.ErrorCode == ErrorCode.Success) ? preBalance.Data.balance.ToString() : "--";
                            subResult.ErrorCode = subTrans.ErrorCode;
                            subResult.RawErrorCode = subTrans.RawErrorCode ?? "";
                            subResult.RawMessage = subTrans.RawMessage ?? "";
                            log2File.Info($"Game To Main : \r\n{JsonConvert.SerializeObject(subResult)}", "AutoTransfer");
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
    }
}
