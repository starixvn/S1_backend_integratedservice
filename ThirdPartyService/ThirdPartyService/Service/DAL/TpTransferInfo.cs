using Dapper;
using Microsoft.AspNetCore.Hosting;
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
    public class TpTransferInfo
    {
        private readonly IConfiguration configuration;
        private readonly Common common;
        private readonly WalletService walletService;

        public TpTransferInfo(IConfiguration configuration, Common common, WalletService walletService)
        {
            this.configuration = configuration;
            this.common = common;
            this.walletService = walletService;
        }

        public ApiResult<TransferResult> TpTransferReq(TpTransfer tpTransfer, TpCustInfo tpCustInfo, string thirdId, out string transId)
        {
            var apiResult = new ApiResult<TransferResult>();
            int transferId = 0;
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                transId = conn.QueryFirstOrDefault<string>("TpTransferInfo_Ins", new { TpId = tpTransfer.TpId, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, SiteId = tpCustInfo.SiteId, Direction = (int)tpTransfer.direction, Amount = tpTransfer.Amount, Memo = (string)null, ThirdId = thirdId, Prefix = $"T{common.GetEnv()}" }, commandType: CommandType.StoredProcedure);
                conn.Open();

                Match m = Regex.Match(transId, @"\d+");
                int.TryParse(m.Value, out transferId);

                if (tpTransfer.direction == Direction.ToTp)
                {
                    using (var trans = conn.BeginTransaction())
                    {
                        decimal afterAmount = -1;
                        var rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount * -1, tpTransfer.TpId, null, transId, out afterAmount, conn, trans);

                        if (rwalletServiceResult != 0)
                        {
                            apiResult.ErrorCode = ErrorCode.MoneyNotEnough;
                            apiResult.Data.Difference = afterAmount;
                            apiResult.Message = "Amount isn't enough";

                            conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = thirdId, Memo = $"amount isn't not enough: {Math.Abs(afterAmount)}", ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }, trans, commandType: CommandType.StoredProcedure);
                            trans.Commit();
                            return apiResult;
                        }
                        else
                        {
                            conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = thirdId, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.PendingD }, trans, commandType: CommandType.StoredProcedure);
                            trans.Commit();
                        }
                    }
                }
            }

            apiResult.ErrorCode = ErrorCode.Success;
            return apiResult;
        }

        public void TpTransferCommit(ApiResult<TransferResult> transferResult, TpCustInfo tpCustInfo, TpTransfer tpTransfer, string transId)
        {
            decimal afterAmount = 0;
            int transferId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out transferId);

            if (transferResult.Data.Status == TransferStatus.Success && transferResult.ErrorCode == ErrorCode.Success)
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        int forStatus = 0;
                        forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;

                        bool isSuccess = conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, transferResult.Data.ThirdId, Memo = transferResult.RawMessage, ForStatus = forStatus, Status = (int)TransferStatus.Success }, trans, commandType: CommandType.StoredProcedure) > 0;
                        int rwalletServiceResult = 0;
                        if (tpTransfer.direction == Direction.ToMain && isSuccess)
                        {
                            rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, null, transId, out afterAmount, conn, trans);
                        }

                        if (rwalletServiceResult == 0)
                            trans.Commit();
                    }
                }
            }
            else if (transferResult.Data.Status == TransferStatus.Fail)
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    using (var trans = conn.BeginTransaction())
                    {
                        int forStatus = 0;
                        forStatus = tpTransfer.direction == Direction.ToTp ? (int)TransferStatus.PendingD : (int)TransferStatus.Pending;
                        string Memo = null;
                        if (transferResult.ErrorCode == ErrorCode.MoneyNotEnough)
                        {
                            Memo = "3RD wallet amount isn't enough";
                        }
                        else if (!string.IsNullOrEmpty(transferResult.RawMessage))
                        {
                            Memo = transferResult.RawMessage;
                        }

                        bool isSuccess = conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, transferResult.Data.ThirdId, Memo = transferResult.RawMessage, ForStatus = forStatus, Status = (int)TransferStatus.Fail }, trans, commandType: CommandType.StoredProcedure) > 0;
                        int rwalletServiceResult = 0;
                        if (tpTransfer.direction == Direction.ToTp && isSuccess)
                        {
                            Memo = $"\r\nreturn amount. Cause: {transferResult.RawMessage}";
                            rwalletServiceResult = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, tpTransfer.Amount, tpTransfer.TpId, Memo, transId, out afterAmount, conn, trans);
                        }

                        trans.Commit();
                    }
                }
            }
        }

        public IEnumerable<TpTransferInfoModel> TpTransferPendingGet()
        {
            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Query<TpTransferInfoModel>("TpTransferInfo_Pending_Get", commandType: CommandType.StoredProcedure);
            }
        }

        public bool SetTransferFail(string transId)
        {
            int transferId = 0;
            Match m = Regex.Match(transId, @"\d+");
            int.TryParse(m.Value, out transferId);

            using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
            {
                return conn.Execute("TpTransferInfo_Set", new { TransferId = transferId, ThirdId = (string)null, Memo = (string)null, ForStatus = (int)TransferStatus.Pending, Status = (int)TransferStatus.Fail }, commandType: CommandType.StoredProcedure) > 0;
            }
        }
    }
}
