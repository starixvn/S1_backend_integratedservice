using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpTransferBonusInfoService
    {
        private readonly string _connectionString;
        private readonly Common common;
        private readonly BonusService bonusService;
        private readonly WalletService _walletService;
        private readonly MySqlClient _mainDbClient;
        public TpTransferBonusInfoService(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _connectionString = configuration.GetConnectionString("ConnectionString");
            common = provider.GetRequiredService<Common>();
            bonusService = provider.GetRequiredService<BonusService>();
            _walletService = provider.GetRequiredService<WalletService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public ApiResult<TransferResult> TpTransferBonusReq(TpBonusTransfer tpBonusTransfer, TpCustInfo tpCustInfo,
            string thirdId, out string transId, out int BonusId)
        {
            var apiResult = new ApiResult<TransferResult>();
            transId = _mainDbClient.QueryFirst<string>("Service_TpTransferBonusInfo_Ins",
                new DynamicParameters(new
                {
                    tpBonusTransfer.TpId,
                    tpCustInfo.CustId,
                    tpCustInfo.UserName,
                    tpCustInfo.SiteId,
                    Memo = (string)null,
                    ThirdId = thirdId,
                    Prefix = $"V{common.GetEnv()}"
                }));
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);
            
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            var rwalletServiceResult = bonusService.BonusTransferOutAll(tpCustInfo.CustId, tpCustInfo.SiteId,
                tpCustInfo.UserName, tpBonusTransfer.TpId, null, transId, out BonusId, out var Bonus,
                out var LockedAmount, out var BonusAmount, out var RolloverAmount, out var ExpiryTime, conn, trans);
            tpBonusTransfer.Bonus = Bonus;
            tpBonusTransfer.LockedAmount = LockedAmount;
            tpBonusTransfer.BonusAmount = BonusAmount;
            tpBonusTransfer.ExpiryTime = ExpiryTime;
            tpBonusTransfer.RolloverAmount = RolloverAmount;

            if (!rwalletServiceResult)
            {
                apiResult.ErrorCode = ErrorCode.Fail;
                apiResult.Data.Difference = 0;
                apiResult.Message = "No bonus can be transferred.";

                _mainDbClient.Execute("Service_TpTransferBonusInfo_Set",
                    new
                    {
                        TransferBonusId = transferId,
                        tpBonusTransfer.Bonus,
                        tpBonusTransfer.BonusAmount,
                        tpBonusTransfer.LockedAmount,
                        tpBonusTransfer.RolloverAmount,
                        ExpiredDate = tpBonusTransfer.ExpiryTime,
                        ThirdId = thirdId,
                        Memo = $"{apiResult.Message}",
                        ForStatus = (int)TransferStatus.Pending,
                        Status = (int)TransferStatus.Fail
                    }, conn, trans);
                trans.Commit();
                return apiResult;
            }

            _mainDbClient.Execute("Service_TpTransferBonusInfo_Set",
                new DynamicParameters(new
                {
                    TransferBonusId = transferId,
                    tpBonusTransfer.Bonus,
                    tpBonusTransfer.BonusAmount,
                    tpBonusTransfer.LockedAmount,
                    tpBonusTransfer.RolloverAmount, 
                    ExpiredDate = tpBonusTransfer.ExpiryTime,
                    ThirdId = thirdId, 
                    Memo = (string)null, 
                    ForStatus = (int)TransferStatus.Pending,
                    Status = (int)TransferStatus.PendingD
                }), conn, trans);
            trans.Commit();

            apiResult.ErrorCode = ErrorCode.Success;
            return apiResult;
        }

        public void TpTransferBonusCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpBonusTransfer tpBonusTransfer, string transId, int bonusId)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                var isSuccess = _mainDbClient.Execute("Service_TpTransferBonusInfoStatus_Set", new
                {
                    TransferBonusId = transferId,
                    transferResult.Data.ThirdId,
                    Memo = transferResult.RawMessage,
                    ForStatus = (int)TransferStatus.PendingD,
                    Status = (int)TransferStatus.Success
                }, conn, trans) > 0;

                if (isSuccess)
                {
                    bonusService.SetBonusStatus(bonusId, 5, 1, conn, trans);
                }

                if (isSuccess)
                    trans.Commit();
            }
            else if (transferResult.Data.Status == TransferStatus.Fail )
            {
                string memo = null;
                if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                {
                    memo = "3RD wallet amount isn't enough";
                }
                else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new
                {
                    TransferBonusId = transferId, 
                    transferResult.Data.ThirdId, 
                    ForStatus = (int)TransferStatus.PendingD,
                    Status = (int)TransferStatus.Fail
                });
                p.Add("Memo", memo, DbType.String, ParameterDirection.Input, 100);
                var isSuccess = _mainDbClient.Execute("Service_TpTransferBonusInfoStatus_Set", p, conn, trans) > 0;
                var rwalletServiceResult = false;
                if (isSuccess)
                {
                    memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                    rwalletServiceResult = bonusService.BonusTransferRollback(tpCustInfo.CustId, tpCustInfo.SiteId,
                        tpCustInfo.UserName, tpBonusTransfer.Bonus, tpBonusTransfer.TpId, memo, transId, bonusId,
                        out _, conn, trans);
                }

                if (rwalletServiceResult)
                    trans.Commit();
            }
            else if (transferResult.Data.Status == TransferStatus.Pending)
            {
                string memo = null;
                if (!string.IsNullOrEmpty(transferResult.RawMessage)) 
                {
                    memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new
                {
                    TransferBonusId = transferId, 
                    transferResult.Data.ThirdId, 
                    ForStatus = (int)TransferStatus.PendingD,
                    Status = (int)TransferStatus.Pending
                });
                p.Add("Memo", memo, DbType.String, ParameterDirection.Input, 100);
                var isSuccess = _mainDbClient.Execute("Service_TpTransferBonusInfoStatus_Set", p, conn, trans) > 0;
                var rwalletServiceResult = false;
                if (isSuccess)
                {
                    memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                    rwalletServiceResult = bonusService.BonusTransferRollback(tpCustInfo.CustId, tpCustInfo.SiteId,
                        tpCustInfo.UserName, tpBonusTransfer.Bonus, tpBonusTransfer.TpId, memo, transId, bonusId,
                        out _, conn, trans);
                }

                if (rwalletServiceResult)
                    trans.Commit();
            }
        }

        public void TpTransferBonusCheckCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpBonusTransfer tpBonusTransfer, string transId, int bonusId)
        {
            var m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out var transferId);
            
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();
            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                //歸零
                var rWalletServiceResult = bonusService.BonusTransferOutAll(tpCustInfo.CustId, tpCustInfo.SiteId,
                    tpCustInfo.UserName, tpBonusTransfer.TpId, null, transId, out _, out _, out _, out _, out _, out _, conn, trans);

                if (rWalletServiceResult)
                {
                    var isSuccess = _mainDbClient.Execute("Service_TpTransferBonusInfoStatus_Set", new
                    {
                        TransferBonusId = transferId,
                        transferResult.Data.ThirdId,
                        Memo = transferResult.RawMessage,
                        ForStatus = (int)TransferStatus.Pending,
                        Status = (int)TransferStatus.Success
                    }, conn, trans) > 0;

                    if (isSuccess)
                    {
                        bonusService.SetBonusStatus(bonusId, 5, 1, conn, trans);
                    }

                    if (isSuccess)
                        trans.Commit();
                }
                
            }
            else if (transferResult.Data.Status == TransferStatus.Pending || transferResult.Data.Status == TransferStatus.Fail)
            {
                string memo = null;
                if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    memo = transferResult.RawMessage;
                }
                var p = new DynamicParameters(new
                {
                    TransferBonusId = transferId, 
                    transferResult.Data.ThirdId, 
                    ForStatus = (int)TransferStatus.Pending,
                    Status = (int)transferResult.Data.Status
                });
                p.Add("Memo", memo, DbType.String, ParameterDirection.Input, 100);
                var isSuccess = _mainDbClient.Execute("Service_TpTransferBonusInfoStatus_Set", p, conn, trans) > 0;
                if (isSuccess) 
                    trans.Commit();
            }
        }
        
        public void TpTransferBonusCancelCommit(TpBonusCancelTransfer tpBonusCancelTransfer, ApiResult<TransferResult> transferResult)
        {
            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                using var conn = new MySqlConnection(_connectionString);
                conn.Open();
                using var trans = conn.BeginTransaction();

                var result = _mainDbClient.Execute("Service_BonusExpired_Set",
                    new
                    {
                        tpBonusCancelTransfer.BonusId,
                        tpBonusCancelTransfer.CustId,
                        tpBonusCancelTransfer.UserName,
                        tpBonusCancelTransfer.SiteId,
                        TYPE = (int)FinancialType.CancelledBonus,
                        tpBonusCancelTransfer.PromoCategoryId,
                        Bonus = tpBonusCancelTransfer.RemainingBonus
                    }, conn, trans);

                if (result >= 0 && tpBonusCancelTransfer.RemainingBonus > 0)
                {
                    var memo = $"Cancel Bonus: BonusId={tpBonusCancelTransfer.BonusId}, Remaining Bonus={tpBonusCancelTransfer.RemainingBonus:#,##0.##}, " +
                               $"Refund Amount={tpBonusCancelTransfer.RefundAmount:#,##0.##}";

                    result = _walletService.SetBalance(
                        tpBonusCancelTransfer.CustId,
                        tpBonusCancelTransfer.SiteId,
                        tpBonusCancelTransfer.UserName,
                        tpBonusCancelTransfer.RefundAmount - tpBonusCancelTransfer.RemainingBonus,
                        tpBonusCancelTransfer.TpId,
                        memo,
                        tpBonusCancelTransfer.BonusId.ToString(), 
                        out _, conn, trans);

                    if (result >= 0 && tpBonusCancelTransfer.RefundAmount > 0)
                    {
                        result = _walletService.SetWithdrawalLimit(tpBonusCancelTransfer.CustId,
                            tpBonusCancelTransfer.UserName,
                            tpBonusCancelTransfer.SiteId, 
                            tpBonusCancelTransfer.RefundAmount, 
                            memo,
                            tpBonusCancelTransfer.BonusId.ToString(),
                            out _, conn, trans);
                    }
                }

                if (result >= 0)
                {
                    trans.Commit();
                }
            }
        }
    }
}
