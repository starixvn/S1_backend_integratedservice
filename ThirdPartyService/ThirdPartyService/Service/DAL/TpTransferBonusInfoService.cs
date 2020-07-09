using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class TpTransferBonusInfoService
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly BonusWalletService bonusWalletService;
        public TpTransferBonusInfoService(IConfiguration configuration, Common common, BonusWalletService bonusWalletService)
        {
            this.configuration = configuration;
            this.common = common;
            this.bonusWalletService = bonusWalletService;
        }

        public ApiResult<TransferResult> TpTransferBonusReq(TpBonusTransfer tpBonusTransfer, TpCustInfo tpCustInfo, string thirdId, out string transId)
        {
            var apiResult = new ApiResult<TransferResult>();
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                transId = conn.ExecuteScalar<string>("Service_TpTransferBonusInfo_Ins", new { TpId = tpBonusTransfer.TpId, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, SiteId = tpCustInfo.SiteId, Memo = (string)null, ThirdId = thirdId, Prefix = $"V{common.GetEnv()}" }, commandType: CommandType.StoredProcedure);
                conn.Open();

                Match m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out int transferId);

                using var trans = conn.BeginTransaction();
                var rwalletServiceResult = bonusWalletService.SetBonusTransferOutAll(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpBonusTransfer.TpId, null, transId, out decimal Bonus, out decimal LockedAmount, out decimal BonusAmount, out long ExpiryTime, conn, trans);
                tpBonusTransfer.Bonus = Bonus;
                tpBonusTransfer.LockedAmount = LockedAmount;
                tpBonusTransfer.BonusAmount = BonusAmount;
                tpBonusTransfer.ExpiryTime = ExpiryTime;

                if (!rwalletServiceResult)
                {
                    apiResult.ErrorCode = ErrorCode.Fail;
                    apiResult.Data.Difference = 0;
                    apiResult.Message = "No bonus can be transferred.";

                    conn.Execute("Service_TpTransferBonusInfo_Set", new { TransferBonusId = transferId, Bonus = tpBonusTransfer.Bonus, BonusAmount = tpBonusTransfer.BonusAmount, LockedAmount = tpBonusTransfer.LockedAmount, ExpiryTime = tpBonusTransfer.ExpiryTime, ThirdId = thirdId, Memo = $"{apiResult.Message}", ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }, trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();
                    return apiResult;
                }
                else
                {
                    conn.Execute("Service_TpTransferBonusInfo_Set", new { TransferBonusId = transferId, Bonus = tpBonusTransfer.Bonus, BonusAmount = tpBonusTransfer.BonusAmount, LockedAmount = tpBonusTransfer.LockedAmount, ExpiryTime = tpBonusTransfer.ExpiryTime, ThirdId = thirdId, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.PendingD }, trans, commandType: CommandType.StoredProcedure);
                    trans.Commit();
                }
            }

            apiResult.ErrorCode = ErrorCode.Success;
            return apiResult;
        }

        public void TpTransferBonusCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpBonusTransfer tpBonusTransfer, string transId)
        {
            int transferId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out transferId);

            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                int forStatus = 0;
                forStatus = (int)TransferStatus.PendingD;

                bool isSuccess = conn.Execute("Service_TpTransferBonusInfo_Set", new { TransferBonusId = transferId, transferResult.Data.ThirdId, Memo = transferResult.RawMessage, ForStatus = forStatus, Status = (int)TransferStatus.Success }, trans, commandType: CommandType.StoredProcedure) > 0;

                if (isSuccess)
                    trans.Commit();
            }
            else if (transferResult.Data.Status == TransferStatus.Fail)
            {
                using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                int forStatus = 0;
                forStatus = (int)TransferStatus.PendingD;
                string Memo = null;
                if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                {
                    Memo = "3RD wallet amount isn't enough";
                }
                else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                {
                    Memo = transferResult.RawMessage;
                }

                var p = new DynamicParameters(new { TransferId = transferId, transferResult.Data.ThirdId, ForStatus = forStatus, Status = (int)TransferStatus.Fail });
                p.Add("Memo", Memo, dbType: DbType.String, direction: ParameterDirection.Input, size: 100);
                bool isSuccess = conn.Execute("Service_TpTransferBonusInfo_Set", p, trans, commandType: CommandType.StoredProcedure) > 0;
                bool rwalletServiceResult = false;
                if (isSuccess)
                {
                    Memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                    rwalletServiceResult = bonusWalletService.SetBonusBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpBonusTransfer.Bonus, tpBonusTransfer.TpId, Memo, transId, out decimal afterBonus, conn, trans);
                }

                if (rwalletServiceResult)
                    trans.Commit();
            }
        }
    }
}
