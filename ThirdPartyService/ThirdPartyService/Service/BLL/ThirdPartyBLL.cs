using Microsoft.AspNetCore.Hosting;
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
        private readonly TpCustInfoService tpCustInfoService;
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly TpTransferInfo tpTransferInfo;
        private readonly IWebHostEnvironment env;

        public ThirdPartyBLL(IWebHostEnvironment env, TpCustInfoService tpCustInfoService, ThirdPartyHelper thirdPartyHelper, TpTransferInfo tpTransferInfo)
        {
            this.tpCustInfoService = tpCustInfoService;
            this.thirdPartyHelper = thirdPartyHelper;
            this.tpTransferInfo = tpTransferInfo;
            this.env = env;
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
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(thirdPartyHelper.GetThirdPartyModel(tpInfo.TpId).Setting);
                if (tpInfo.inputs != null)
                {
                    foreach (var infoI in tpInfo.inputs)
                    {
                        inputs.Add(infoI.Key, infoI.Value);
                    }
                }
                apiResult = tpHelper.Login(tpCustInfoResult.Data, (AppType)tpInfo.AppType, tpLangId, inputs);
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
            //if (tpCustInfoResult.Data.IsTest && env.EnvironmentName == "Production")
            //{
            //    apiResult.ErrorCode = ErrorCode.NoAuthority;
            //    apiResult.RawMessage = "This Account is for Testing.";
            //    return apiResult;
            //}
            //else if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            if (tpCustInfoResult.ErrorCode != ErrorCode.Success)
            {
                apiResult.ErrorCode = tpCustInfoResult.ErrorCode;
                apiResult.RawErrorCode = tpCustInfoResult.RawErrorCode;/*3RD Error Code*/
                apiResult.RawMessage = tpCustInfoResult.RawMessage;/*3RD Error Message*/
                return apiResult;
            }

            IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpTransfer.TpId);
            var oThirdPartyModel = thirdPartyHelper.GetThirdPartyModel(tpTransfer.TpId);
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
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyModel(tpInfo.TpId);
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
            if (tpCustInfoResult.ErrorCode == ErrorCode.Success)
            {
                var tpHelper = (IThirdPartyParticular)thirdPartyHelper.GetThirdPartyHelper(tpInfo.TpId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyModel(tpInfo.TpId);
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
                IThirdPartyHelper tpHelper = thirdPartyHelper.GetThirdPartyHelper(tpTransferInfoModel.TpId);
                var oThirdPartyModel = thirdPartyHelper.GetThirdPartyModel(tpTransferInfoModel.TpId);
                var inputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(oThirdPartyModel.Setting);
                var tpCustInfoResult = tpCustInfoService.GetTpCustInfo(tpTransferInfoModel.CustId, tpTransferInfoModel.TpId);

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
    }
}
